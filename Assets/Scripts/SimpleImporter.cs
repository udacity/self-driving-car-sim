using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;

namespace PointCloudExporter
{
	public class SimpleImporter
	{
		// Singleton
		private static SimpleImporter instance;
		private SimpleImporter () {}
		public static SimpleImporter Instance {
			get {
				if (instance == null) {
					instance = new SimpleImporter();
				}
				return instance;
			}
		}

		public MeshInfos Load (string filePath, int maximumVertex = 65000)
		{
			MeshInfos data = new MeshInfos();
			int levelOfDetails = 1;
			if (File.Exists(filePath)) {
				using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open))) {
					int cursor = 0;
					int length = (int)reader.BaseStream.Length;
					string lineText = "";
					bool header = true;
					int vertexCount = 0;
					int colorDataCount = 3;
					int index = 0;
					int step = 0;
					while (cursor + step < length) {
						if (header) {
							char v = reader.ReadChar();
							if (v == '\n') {
								if (lineText.Contains("end_header")) {
									header = false;
								} else if (lineText.Contains("element vertex")) {
									string[] array = lineText.Split(' ');
									if (array.Length > 0) {
										int subtractor = array.Length - 2;
										vertexCount = Convert.ToInt32 (array [array.Length - subtractor]);
										if (vertexCount > maximumVertex) {
											levelOfDetails = 1 + (int)Mathf.Floor(vertexCount / maximumVertex);
											vertexCount = maximumVertex;
										}
										data.vertexCount = vertexCount;
										data.vertices = new Vector3[vertexCount];
										data.normals = new Vector3[vertexCount];
										data.colors = new Color[vertexCount];
									}
								} else if (lineText.Contains("property uchar alpha")) {
									colorDataCount = 4;
								}
								lineText = "";
							} else {
								lineText += v;
							}
							step = sizeof(char);
							cursor += step;
						} else {
							if (index < vertexCount) {

								data.vertices[index] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
								data.normals[index] = new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
								data.colors[index] = new Color(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, 1f);

								step = sizeof(float) * 6 * levelOfDetails + sizeof(byte) * colorDataCount * levelOfDetails;
								cursor += step;
								if (colorDataCount > 3) {
									reader.ReadByte();
								}

								if (levelOfDetails > 1) { 
									for (int l = 1; l < levelOfDetails; ++l) { 
										for (int f = 0; f < 6; ++f) { 
											reader.ReadSingle(); 
										} 
										for (int b = 0; b < colorDataCount; ++b) { 
											reader.ReadByte(); 
										} 
									} 
								} 

								++index;
							}
						}
					}
				}
			}
			return data;
		}
	}
}
