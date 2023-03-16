// --------------- FlattenedRender/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 2) in vec3 inNormal;

out vec3 normal;
out vec3 colour;

void main()
{
    // adjust normal relative to rotation / scale
    normal = lx_NormalFix(lx_Model,inNormal);
    
    // relatively random colour by dividing by primes (mod 1)
    colour = normalize(vec3(mod(float(gl_VertexID) / 751.0, 1.0) + 0.2, mod(float(gl_VertexID) / 443.0, 1.0) + 0.1, mod(float(gl_VertexID) / 1217.0, 1.0)) + 0.3);
    
    // transform our 3D model
    gl_Position = lx_Model*vec4(inPosition, 1.0);
    gl_Position.z = 0.0; // set z to 0 so everything is on screen
}