#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 2) in vec3 aNormal;

out vec3 fragPos;
out vec3 normal;
out vec3 colour;


void main()
{
    normal = lx_NormalFix(lx_Model,aNormal);
    fragPos = aPos;
    
    // relatively random colour by dividing by primes
    colour = normalize(vec3(mod(float(gl_VertexID) / 751.0, 1.0) + 0.2, mod(float(gl_VertexID) / 443.0, 1.0) + 0.1, mod(float(gl_VertexID) / 1217.0, 1.0)) + 0.3);
    
    gl_Position = lx_Model*vec4(aPos, 1.0);
    gl_Position.z = 0.0;
}