#version luma-dx

uniform lx_Material material;
uniform lx_Light light;
uniform sampler2D normalMap;

in VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 tangent;
} fs_in;


void main()
{
    vec3 normal = lx_NormalMap(normalMap,fs_in.texCoords);
    vec3 phong = lx_Phong(normal, fs_in.TBNfragPos, vec3(0.0), fs_in.texCoords, fs_in.texCoords, material, lx_MoveLight(light,fs_in.TBNlightPos), 1.0);
    lx_FragColour = lx_Colour(phong);
}