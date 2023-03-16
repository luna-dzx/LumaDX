// --------------- FullModelRender/fragment.glsl --------------- //

#version luma-dx

uniform sampler2D dingus;

in vec3 fragPos;
in vec3 normal;
in vec2 texCoords;

flat in int reject;

uniform vec3 colour = vec3(1.0,0.0,0.0);

[textured]
void main()
{
    if (reject == 1) discard;
    lx_FragColour = texture(dingus,texCoords);
}

[coloured]
void main()
{
    if (reject == 1) discard;
    lx_FragColour = vec4(abs(normal),1.0);
}

[flat]
void main()
{
    if (reject == 1) discard;
    lx_FragColour = vec4(colour,1.0);
}