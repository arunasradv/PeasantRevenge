import os
import time
import subprocess

AssemblyVersion = "28.0.0.0"
SupportedGameVersion = "1.4.8"
FileVersion = AssemblyVersion

def get_commit_info():
    result = subprocess.run(['git', 'show', '--no-patch', '--no-notes', '--date=short', 'HEAD'], stdout=subprocess.PIPE)
    output = result.stdout.decode().strip()
    lines = output.split('\n')
    #commit_hash = lines[0].strip()
    #commit_date = lines[1].strip()
    return lines

Timestamp = time.strftime("%Y-%m-%d")
commit_message = get_commit_info()[4]
file_dirr = os.path.dirname(os.path.abspath(__file__))

#Updating changelog.txt
found_changelog = False
with open(os.path.join(file_dirr, "changelog.txt"), "r") as f:
    lines = f.readlines()
    for line in lines:
        if f"Mod version: {AssemblyVersion}. Game version: {SupportedGameVersion}." in line:
            found_changelog = True
            break
    if not found_changelog:
        lines.append(f"\n{Timestamp} Mod version: {AssemblyVersion}. Game version: {SupportedGameVersion}.\n")
        #Append the last git commit message to the changelog   
        lines.append(f" {commit_message}\n")
    else: #Replace the last git commit message in the changelog
        for i in range(len(lines)):
            if f"Mod version: {AssemblyVersion}. Game version: {SupportedGameVersion}." in lines[i]:
                lines[i+1] = f" {commit_message}\n"
                break
with open(os.path.join(file_dirr, "changelog.txt"), "w") as f:
    f.writelines(lines)

#Updating PeasantRevenge.csproj
with open(os.path.join(file_dirr, "PeasantRevenge.csproj"), "r") as f:
    lines = f.readlines()
    for i in range(len(lines)):
        if "<AssemblyVersion>" in lines[i]:
            lines[i] = f"    <AssemblyVersion>{AssemblyVersion}</AssemblyVersion>\n"
        if "<FileVersion>" in lines[i]:
            lines[i] = f"    <FileVersion>{FileVersion}</FileVersion>\n"
with open(os.path.join(file_dirr, "PeasantRevenge.csproj"), "w") as f:
    f.writelines(lines)

#Updating AssemblyInfo.cs
with open(os.path.join(file_dirr, "Properties/AssemblyInfo.cs"), "r") as f:
    lines = f.readlines()
    for i in range(len(lines)):
        if "[assembly: AssemblyVersion" in lines[i]:
            lines[i] = f"[assembly: AssemblyVersion(\"{AssemblyVersion}\")]\n"
        if "[assembly: AssemblyFileVersion" in lines[i]:
            lines[i] = f"[assembly: AssemblyFileVersion(\"{FileVersion}\")]\n"
with open(os.path.join(file_dirr, "Properties/AssemblyInfo.cs"), "w") as f:
    f.writelines(lines)

#Updating WorkshopUpdate.xml
with open(os.path.join(file_dirr, "WorkshopUpdate.xml"), "r") as f:
    lines = f.readlines()
    for i in range(len(lines)):
        if "ChangeNotes" in lines[i]:
            lines[i] = f"        <ChangeNotes Value=\"Mod version: {AssemblyVersion}. Game version: {SupportedGameVersion}.\"/>\n"
        if "<Tag Value=\"v" in lines[i]:
            lines[i] = f"        <Tag Value=\"v{SupportedGameVersion}\"/>\n"
with open(os.path.join(file_dirr, "WorkshopUpdate.xml"), "w") as f:
    f.writelines(lines)

#Updating SubModule.xml
with open(os.path.join(file_dirr, "SubModule.xml"), "r") as f:
    lines = f.readlines()
    for i in range(len(lines)):
        if "=\"v" in lines[i] and "Version" in lines[i]:
            lines[i] = lines[i].split("=\"v")[0] + f"=\"v{SupportedGameVersion}\"/>\n"  
        if "=\"v" in lines[i] and "DependedModule" in lines[i]:
            lines[i] = lines[i].split("=\"v")[0] + f"=\"v{SupportedGameVersion}.0\"/>\n"               
with open(os.path.join(file_dirr, "SubModule.xml"), "w") as f:
    f.writelines(lines)

print(f"Updated changelog.txt, PeasantRevenge.csproj, AssemblyInfo.cs, WorkshopUpdate.xml, SubModule.xml with new versions: {AssemblyVersion}, {FileVersion}, {SupportedGameVersion} and timestamp: {Timestamp}")
