#version luma-dx

uniform samplerCube cubeMap;


in vec2 texCoords;

void main()
{
    // tile 4x3 squares to make a cube net
    vec2 coords = texCoords * vec2(4.0,3.0);
    
    // reject corners
    if ((coords.y <= 1.0 || coords .y >= 2.0) && (coords.x <= 1.0 || coords.x > 2.0)) discard;
    
    // coordinates between -1.0 and 1.0 for each square
    vec3 localCoords = vec3(mod(coords,1.0) * 2.0 - 1.0, 1.0);
    
    // top and bottom of net
    if (coords.y > 2.0) { lx_FragColour = vec4(texture(cubeMap, vec3(localCoords.x,1.0,localCoords.y)).r); return; }
    if (coords.y <= 1.0) { lx_FragColour = vec4(texture(cubeMap, vec3(localCoords.x,-1.0,localCoords.y*-1.0)).r); return; }
    
    // middle pieces of net
    if (coords.x <= 1.0) lx_FragColour = vec4(texture(cubeMap, vec3(-1.0, localCoords.yx * vec2(1.0,-1.0))).r);
    if (coords.x > 1.0 && coords.x <= 2.0) lx_FragColour = vec4(texture(cubeMap, vec3(localCoords.xy, -1.0)).r);
    if (coords.x > 2.0 && coords.x <= 3.0) lx_FragColour = vec4(texture(cubeMap, vec3(1.0, localCoords.yx)).r);
    if (coords.x > 3.0) lx_FragColour = vec4(texture(cubeMap, vec3(localCoords.xy * vec2(-1.0,1.0), 1.0)).r);
}