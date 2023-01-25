#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in mat4 instanceMatrix;

out vec3 fragPos;
out vec3 normal;
out vec3 textureDir;

[scene]
void main()
{
    normal = lx_NormalFix(lx_Model * instanceMatrix,aNormal);
    fragPos = vec3(lx_Model * instanceMatrix * vec4(aPos, 1.0));
    gl_Position = lx_Transform * instanceMatrix * vec4(aPos , 1.0);
}

[skyBox]

void main()
{
    textureDir = aPos;
    mat4 view = mat4(mat3(lx_View));
    gl_Position = (lx_Proj * view *vec4(aPos, 1.0)).xyww; // this sets z to w/w = 1.0 for maximum depth
}