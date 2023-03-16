// --------------- LightCasters/fragment.glsl --------------- //

#version luma-dx

uniform lx_Light light;
uniform lx_Material material;

in vec3 fragPos;
in vec3 normal;

uniform vec3 cameraPos;

[scene]
void main()
{
    lx_FragColour = lx_Colour( lx_Phong(normal, fragPos, cameraPos, material, light, 1.0) );
}

[light]
void main()
{
    lx_FragColour = vec4(light.diffuse,1.0);
}
