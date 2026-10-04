#!/usr/bin/env python3
"""Prepare a published static build, including hashes for Blazor's import map."""
import argparse
import base64
import hashlib
from pathlib import Path
import re

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('wwwroot', type=Path)
parser.add_argument('--base-path', default='/steam-review-forge/')
args = parser.parse_args()
if not re.fullmatch(r'/[A-Za-z0-9_/-]*', args.base_path) or not args.base_path.endswith('/'):
    parser.error('--base-path must be an absolute directory path ending in /')
index = args.wwwroot / 'index.html'
html = index.read_text(encoding='utf-8')
html = re.sub(r'<base href="[^"]*"\s*/>', f'<base href="{args.base_path}" />', html)
html = re.sub(r'\s*<meta http-equiv="Content-Security-Policy"[^>]*>', '', html)
hashes = []
for attributes, script in re.findall(r'<script\b([^>]*)>(.*?)</script>', html, re.S):
    if not re.search(r'\bsrc\s*=', attributes):
        digest = base64.b64encode(hashlib.sha256(script.encode('utf-8')).digest()).decode('ascii')
        hashes.append(f"'sha256-{digest}'")
policy = '; '.join([
    "default-src 'none'",
    "script-src 'self' 'wasm-unsafe-eval' " + ' '.join(hashes),
    "style-src 'self' 'unsafe-inline'",
    "font-src 'self'",
    "img-src 'self' data:",
    "connect-src 'self'",
    "worker-src 'self' blob:",
    "base-uri 'self'",
    "object-src 'none'",
    "form-action 'none'",
])
html = html.replace('<head>', '<head>\n    <meta http-equiv="Content-Security-Policy" content="' + policy + '" />', 1)
index.write_text(html, encoding='utf-8')
(args.wwwroot / '404.html').write_text(html, encoding='utf-8')
(args.wwwroot / '.nojekyll').touch()
print(f'Prepared {index} with base {args.base_path} and a hashed script policy.')
