// -------------------- geometryCubeMap.glsl -------------------- //

#version 330 core

layout (triangles) in;
layout (triangle_strip, max_vertices=18) out;

uniform mat4 shadowMatrices[6];

out vec3 cubePos; // outputs 6 times, one for each side

void main()
{
    for (int cubeSide = 0; cubeSide < 6; cubeSide++)
    {
        gl_Layer = cubeSide;
        for (int i = 0; i < 3; i++)
        {
            cubePos = gl_in[i].gl_Position.xyz;
            gl_Position = shadowMatrices[cubeSide] * gl_in[i].gl_Position;
            EmitVertex();
        }
        
        EndPrimitive();
    }
}