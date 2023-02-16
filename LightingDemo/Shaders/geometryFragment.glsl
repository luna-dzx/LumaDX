#version luma-dx

uniform lx_Material material;
uniform sampler2D normalMap;

layout (location = 0) out vec3 gPosition;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec2 gTexCoords;

in VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
} fs_in;

[backpack]
void main()
{
    gPosition = fs_in.fragPos;
    gNormal = normalize(fs_in.normal);
    
    gTexCoords = fs_in.texCoords;
}

[cube]
void main()
{
    gPosition = fs_in.fragPos;
    gNormal = normalize(fs_in.normal);
    
    gTexCoords = vec2(-1,-1); // for demo purposes to display the cube as white, in reality you would never do this
}