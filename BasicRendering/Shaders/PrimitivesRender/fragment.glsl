// --------------- PrimitivesRender/fragment.glsl --------------- //

#version luma-dx

in vec3 fragPos;

[cube]
void main()
{
    // colour based on position
    lx_FragColour = lx_Colour( (fragPos + 1.4) * 0.4);
}

[quad]
void main()
{
    lx_FragColour = vec4(0.9);
}
