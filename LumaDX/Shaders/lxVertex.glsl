uniform mat4 lx_Model = mat4(1.0);
uniform mat4 lx_View = mat4(1.0);
uniform mat4 lx_Proj = mat4(1.0);
uniform int lx_AutoProjection;
mat4 lx_Transform = mat4(1.0);

vec3 lx_NormalFix(mat4 appliedMatrix, vec3 normal)
{
    return mat3(transpose(inverse(appliedMatrix))) * normal;
}

// tangent,bitangent,normal matrix
mat3 lx_TBN(mat4 model, vec3 tangent, vec3 normal)
{
    vec3 T = normalize( mat3(model) * tangent );
    vec3 N = normalize( mat3(model) * normal );
   
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);
   
    return transpose(mat3(T, B, N));
}

[main]
if (lx_AutoProjection == 1){lx_Transform = lx_Proj * lx_View * lx_Model;}