// --------------- BasicCollisions/fragment.glsl --------------- //

#version luma-dx

uniform sampler2D dingus;
uniform vec3 colour;

in vec2 texCoords;
flat in int drawTexture;
uniform int renderCloseTriangles;

[dingus]
void main()
{
    if (renderCloseTriangles == 0){
        lx_FragColour = texture(dingus,texCoords);
        return;
    }
    if (drawTexture == 1) lx_FragColour = lx_Colour(vec3(1.0,0.0,0.0));
    else lx_FragColour = lx_Colour(0.1);
}

[sphere]
void main()
{
    lx_FragColour = lx_Colour(colour);
}