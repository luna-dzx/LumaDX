// -------------------- vertex.glsl -------------------- //

#version 330 core

const vec4 inPosition[3] = vec4[3](
    vec4(-3,-1,0,1),
    vec4(1,-1,0,1),
    vec4(1,3,0,1)
);
const vec2 inTexCoords[3] = vec2[3](
    vec2(-1, 0),
    vec2(1, 0),
    vec2(1, 2)
);

out vec2 texCoords;

void main()
{
    gl_Position = inPosition[gl_VertexID % 3];
    texCoords = inTexCoords[gl_VertexID % 3]; 
}



// this shader avoids drawing a quad or using vertex arrays by drawing one big triangle over the screen