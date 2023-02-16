#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;

out vec3 fragPos;
out vec3 normal;
out vec2 texCoords;

uniform int numTriangles;
uniform int triangleOffset;

flat out int reject;

void main()
{
    if (numTriangles*3 + triangleOffset <= gl_VertexID){reject = 1;}
    else{reject = 0;}
    
    normal = lx_NormalFix(lx_Model,aNormal);
    fragPos = vec3(lx_Model * vec4(aPos, 1.0));
    gl_Position = lx_Transform * vec4(aPos , 1.0);
    texCoords = aTexCoords;
}