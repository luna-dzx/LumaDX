// --------------- FullModelRender/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;
layout (location = 2) in vec3 inNormal;

out vec3 fragPos;
out vec3 normal;
out vec2 texCoords;

uniform int numTriangles;
uniform int triangleOffset;

flat out int reject;

void main()
{
    // calculate whether this triangle should be rejected based on its VertexID (the iterator that counts for each vertex)
    if (numTriangles*3 + triangleOffset <= gl_VertexID){reject = 1;}
    else{reject = 0;}
    
    normal = lx_NormalFix(lx_Model,inNormal);
    fragPos = vec3(lx_Model * vec4(inPosition, 1.0));
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
    texCoords = inTexCoords;
}