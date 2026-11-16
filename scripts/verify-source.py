#!/usr/bin/env python3
"""Static source/asset checks. This does not compile or certify runtime behavior."""
import hashlib
import json
import re
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src'
EXPECTED_PROJECTS = {'shareW', 'ShareX.Avalonia', 'ShareX.HelpersLib', 'ShareX.Icons',
                     'ShareX.ImageEditor', 'ShareX.ScreenCaptureLib'}
FORBIDDEN = re.compile(r'\b(?:HttpClient|HttpListener|WebClient|WebRequest|Socket|TcpClient|UdpClient|'
                       r'FtpWebRequest|SmtpClient|Ping|PingReply|ProxyInfo|HttpClientFactory|FileDownloader|'
                       r'ProcessStartInfo|UploadCommand|UploadRequested|UploadImageRequested)\b|'
                       r'\b(?:Dns|Process)\s*\.\s*(?:GetHostEntry|GetHostAddresses|Start)\s*\(')


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def run():
    manifest = json.loads((ROOT / 'scripts/source-manifest.json').read_text())
    expected_files = {item['path']: item['sha256'] for item in manifest['files']}
    actual_files = {p.relative_to(ROOT).as_posix(): p for p in SRC.rglob('*')
                    if p.is_file() and not {'bin', 'obj'} & set(p.relative_to(SRC).parts)}
    require(set(expected_files) == set(actual_files), 'Source file list differs from manifest')
    for name, path in actual_files.items():
        require(hashlib.sha256(path.read_bytes()).hexdigest() == expected_files[name],
                'Changed file: ' + name)

    projects = [p for p in actual_files.values() if p.suffix == '.csproj']
    require({p.stem for p in projects} == EXPECTED_PROJECTS, 'Unexpected project graph')
    package_names = set()
    xml_count = 0
    for path in actual_files.values():
        if path.suffix in {'.csproj', '.props', '.targets', '.resx', '.axaml', '.manifest'}:
            ET.parse(path)
            xml_count += 1
    for project in projects:
        tree = ET.parse(project)
        for ref in tree.iter('ProjectReference'):
            target = (project.parent / ref.attrib['Include'].replace('\\', '/')).resolve()
            require(target.is_file() and target.stem in EXPECTED_PROJECTS,
                    'Missing/unapproved project reference: ' + str(target))
        package_names |= {r.attrib['Include'] for r in tree.iter('PackageReference')}
    pinned = {r.attrib['Include'] for r in ET.parse(SRC / 'Directory.Packages.props').iter('PackageVersion')}
    require(package_names == pinned, 'Missing or unused package versions')
    require(not any(re.search(r'FTP|SSH|Onnx|Sqlite|ZXing|DiagnosticsSupport', n, re.I)
                    for n in package_names), 'Unapproved package reference')

    cs_files = [p for p in actual_files.values() if p.suffix == '.cs']
    for path in cs_files:
        text = path.read_text(encoding='utf-8-sig')
        # Ignore comments (license links are not network operations), keeping string literals.
        text = re.sub(r'/\*[\s\S]*?\*/|(?m:^[ \t]*//[^\n]*)', '', text)
        require(not FORBIDDEN.search(text), 'Forbidden API or upload hook: ' + str(path))

    # Ensure the complete upstream tool catalog and native capture host are retained.
    tools_file = SRC / 'ShareX.ImageEditor/Presentation/ViewModels/ToolbarCustomizationItemViewModel.cs'
    tools = re.findall(r'CreateToolDefinition\(EditorTool\.(\w+)', tools_file.read_text())
    require(tools == manifest['editor_tools'], 'Annotation tool catalog changed')
    for item in manifest['unchanged_capture_files']:
        path = SRC / item['path']
        require(hashlib.sha256(path.read_bytes()).hexdigest() == item['upstream_sha256'],
                'Upstream capture implementation changed: ' + item['path'])
    capture_tools = [t for t in tools if t not in {'Crop', 'CutOut'}]
    require(len(capture_tools) == 19 and len(tools) == 21, 'Unexpected tool count')
    host = (SRC / 'shareW/MainWindow.cs').read_text()
    require('EnableAnnotations = true' in host and 'AllowMultipleRegions = true' in host,
            'Capture annotations or multi-region support disabled')

    # Validate ResXFileRef paths without loading or executing resource data.
    resource_refs = 0
    for resx in (p for p in actual_files.values() if p.suffix == '.resx'):
        for node in ET.parse(resx).findall('data'):
            if 'ResXFileRef' not in node.attrib.get('type', ''):
                continue
            value = node.findtext('value', '')
            target = resx.parent / value.split(';')[0].replace('\\', '/')
            require(target.is_file(), 'Missing resource: ' + str(target))
            resource_refs += 1

    print(f'PASS: {len(actual_files)} inventoried files; {len(projects)} projects; '
          f'{len(cs_files)} C# files scanned; {xml_count} XML files; {resource_refs} resource references.')
    print('PASS: 19 capture tools and 21 editor tools retained; original capture host matches upstream.')
    print('NOT TESTED: compilation, Windows behavior, transitive dependencies, or runtime network traffic.')


if __name__ == '__main__':
    try:
        run()
    except (AssertionError, OSError, ValueError, ET.ParseError) as error:
        print('FAIL:', error, file=sys.stderr)
        sys.exit(1)
