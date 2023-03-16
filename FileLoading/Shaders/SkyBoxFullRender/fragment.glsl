// --------------- SkyBoxFullRender/fragment.glsl --------------- //

#version luma-dx

uniform samplerCube cubeMap;

in vec2 texCoords;

uniform vec3 direction;
uniform vec3 tangent;

void main()
{
    float fieldOfView = 160.0;
    vec2 coords = texCoords * fieldOfView/90.0 - vec2(1.5,1.0);
    vec3 biTangent = cross(direction,tangent) * - 1.0;
    
    // change direction based on pixel coordinate
    lx_FragColour = texture(cubeMap,normalize(direction + tangent * coords.x + biTangent * coords.y));
}