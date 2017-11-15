## PCD Map Builder

This branch contains the source code to create Unity maps with rendering and collision features from PCD files. The code is contained in the scene "self-driving-car-sim/Assets/1_SelfDrivingCar/Scenes/PCDMapCreator_export.unity" and uses the script "self-driving-car-sim/Assets/1_SelfDrivingCar/Scripts/PointCloudGenerator.cs".

Built applications for PC, MAC, and Linux are also included in the RELEASE page. Also included in this branch is serveral PCD files to show how formatting works. Each PCD was recorded with a Velodyne 16 Lidar.

test_lot.txt        { Udacity test parking lot, contains just x,y,z } (script only uses (x,y,z) so columns [0][1][2] of each line.

test_lot_normals_combined.txt     { Udacity test parking lot, contains x,y,z,normal_x,normal_y,normal_z } (script uses (x,y,z) for columns [0][1][2] and (normal_x,normal_y,normal_z) for columns [3][4][5], also notice each (x,y,z) point is duplicated but with reversing normals so the script generates both side of the triangle

parking_small.txt  { Udacity's parking lot, contains just x,y,z } (script only uses (x,y,z) so columns [0][1][2] of each line.

california_normals_combined.txt   { A large map of a stretch of California Street in Mountain View CA, contains x,y,z,normal_x,normal_y,normal_z,normal_angle } (script uses (x,y,z) for columns [0][1][2] and (normal_x,normal_y,normal_z) for columns [3][4][5], also notice each (x,y,z) point is duplicated but with reversing normals so the script generates both side of the triangle. The normal angle is not used.








