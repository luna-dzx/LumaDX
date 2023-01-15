#version luma-dx

uniform lx_Material material;
uniform lx_Light light;
uniform sampler2D normalMap;
uniform vec3 cameraPos;

in VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 TBNcameraPos;
    vec3 tangent;
} fs_in;

[scene]
void main()
{
    vec3 normal = lx_NormalMap(normalMap,fs_in.texCoords);
    vec3 phong = lx_Phong(normal, fs_in.TBNfragPos, fs_in.TBNcameraPos, fs_in.texCoords, fs_in.texCoords, material, lx_MoveLight(light,fs_in.TBNlightPos), 1.0);
    lx_FragColour = lx_Colour(phong);
}

[light]
void main()
{
    lx_FragColour = vec4(1.0);
}