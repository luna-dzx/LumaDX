#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;

out vec2 texCoords;
flat out int drawTexture;

uniform vec3 playerPos;
uniform vec3 playerRadius;
uniform float triangleDistance;
uniform int renderCloseTriangles;

float lenSquared(vec3 vec)
{
    return dot(vec,vec);
}

void main()
{
    drawTexture = 1;
    
    if (renderCloseTriangles == 1 && lenSquared((lx_Model * vec4(inPosition, 1.0)).xyz/playerRadius - playerPos) > 1.0 + triangleDistance)
    {
        drawTexture = 0;
    }
    texCoords = inTexCoords;
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
}