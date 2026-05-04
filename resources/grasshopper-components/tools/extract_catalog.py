"""Extract installed ghpythonlib component docs into grasshopper-components/legacy-text.

Run inside Rhino/Grasshopper Python where Grasshopper and ghpythonlib are available.
After extraction, run tools/build_catalog.py from normal Python to refresh
components.jsonl and by-plugin/*.jsonl.
"""

import os
import re

import Grasshopper as gh
import ghpythonlib.components as ghc


def safe_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name) + ".txt"


def find_catalog_root():
    try:
        here = os.path.dirname(os.path.abspath(__file__))
        return os.path.dirname(here)
    except Exception:
        return os.path.expanduser("~/Desktop/grasshopper-components")


catalog_root = find_catalog_root()
legacy_folder = os.path.join(catalog_root, "legacy-text")

if not os.path.exists(legacy_folder):
    os.makedirs(legacy_folder)

category_lookup = {}
for proxy in gh.Instances.ComponentServer.ObjectProxies:
    ghc_name = re.sub(r"\W+", "", proxy.Desc.Name)
    category_lookup[ghc_name] = proxy.Desc.Category

grouped_nodes = {}
for node_name in dir(ghc):
    if node_name.startswith("_"):
        continue
    try:
        docs = getattr(ghc, node_name).__doc__
        if not docs:
            continue
        category = category_lookup.get(node_name, "Uncategorized")
        grouped_nodes.setdefault(category, []).append("--- {} ---\n{}\n".format(node_name, docs))
    except Exception:
        pass

for category, nodes in grouped_nodes.items():
    file_path = os.path.join(legacy_folder, safe_filename(category))
    with open(file_path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(nodes))

print("Extraction complete: {} plugin/category files saved to {}".format(len(grouped_nodes), legacy_folder))
