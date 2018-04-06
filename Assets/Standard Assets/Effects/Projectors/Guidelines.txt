To use the Projector/Light and Projector/Shadow shaders properly:

Cookie texture:
	1. Make sure texture wrap mode is set to "Clamp"
	2. Turn on "Border Mipmaps" option in import settings
	3. Use uncompressed texture format
	4. Projector/Shadow also requires alpha channel to be present (typically Alpha from Grayscale option is ok)

Falloff texture (if present):
	1. Data needs to be in alpha channel, so typically Alpha8 texture format
	2. Make sure texture wrap mode is set to "Clamp"
	3. Make sure leftmost pixel column is black; and "Border mipmaps" import setting is on.
