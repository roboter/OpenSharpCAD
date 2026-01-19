import os

def find_old_csproj(root_dir):
    old_projects = []
    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if file.endswith(".csproj"):
                path = os.path.join(root, file)
                try:
                    with open(path, 'r', encoding='utf-8') as f:
                        header = f.read(500)
                        if 'Sdk="Microsoft.NET.Sdk"' not in header:
                            old_projects.append(path)
                except Exception as e:
                    print(f"Error reading {path}: {e}")
    return old_projects

if __name__ == "__main__":
    projects = find_old_csproj("c:\\PROJECTS\\OpenSharpCAD")
    for p in projects:
        print(p)
