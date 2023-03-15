#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;
layout (location = 2) in vec3 inNormal;

out vec3 fragPos;
out vec3 normal;
out vec3 textureDir;
out vec2 texCoords;
flat out int instanceId;

uniform float time;
uniform int dingusCount;

[scene]
void main()
{
    instanceId = gl_InstanceID;
    //             \/ tau
    float theta = 6.28318530718  * float(gl_InstanceID) / float(dingusCount % 35);
    vec3 pos = vec3(sin(theta + 0.1 * time) * 20.0, cos(theta * 2.0 + time*0.3) * 3.0 + 0.1 * float(gl_InstanceID) - 0.05 * float(dingusCount), cos(theta + 0.1 * time) * 20.0);
    vec3 rot = vec3(-1.57079632679, theta * 2.0 + time, 0.0);
    vec3 scale = vec3(0.08);
    mat4 instanceMatrix = lx_CreateTransform(pos,rot,scale);

    normal = lx_NormalFix(lx_Model * instanceMatrix,inNormal);
    fragPos = vec3(lx_Model * instanceMatrix * vec4(inPosition, 1.0));
    gl_Position = lx_Transform * instanceMatrix * vec4(inPosition , 1.0);
    
    texCoords = inTexCoords;
}

[skyBox]

void main()
{
    textureDir = inPosition;
    mat4 view = mat4(mat3(lx_View));
    gl_Position = (lx_Proj * view *vec4(inPosition, 1.0)).xyww; // this sets z to w/w = 1.0 for maximum depth
}