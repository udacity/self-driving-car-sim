import os
import re
from sys import argv
from mod_pbxproj import XcodeProject

path = argv[1]

print path

project = XcodeProject.Load(path +'/Unity-iPhone.xcodeproj/project.pbxproj')
project.add_file_if_doesnt_exist('System/Library/Frameworks/Security.framework', tree='SDKROOT')
project.add_file_if_doesnt_exist('usr/lib/libicucore.dylib', tree='SDKROOT')

# regex for adjust sdk files
re_adjust_files = re.compile(r"SRWebSocket\.m")

# iterate all objects in the unity Xcode iOS project file
for key in project.get_ids():
    
    obj = project.get_obj(key)
    
    name = obj.get('name')
    
    adjust_file_match = re_adjust_files.match(name if name else "")
    
    if (adjust_file_match):
        build_files = project.get_build_files(key)
        for build_file in build_files:
            # add the ARC compiler flag to the adjust file if doesn't exist
            build_file.add_compiler_flag('-fobjc-arc')


if project.modified:
    project.backup()
    project.save()
