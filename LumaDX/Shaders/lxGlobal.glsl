struct lx_Material
{
    sampler2D baseTex;
    sampler2D specTex;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct lx_Light {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    vec3 attenuation;
};


lx_Light lx_MoveLight(lx_Light light, vec3 position)
{
    lx_Light lightTemp = light;
    lightTemp.position = position;
    return lightTemp;
}

lx_Light lx_TransformLight(lx_Light light, mat3 transform)
{
    lx_Light lightTemp = light;
    lightTemp.position = transform*light.position;
    return lightTemp;
}

float lx_NormalFlip(vec3 position, vec3 normal)
{
    if (dot (normal, position) < 0.0) // approximation, almost always true
    {
        return - 1.0;
    }
    return 1.0;
}

vec3 lx_NormalFlipVec(vec3 position, vec3 normal)
{
    if (dot (normal, position) < 0.0) // approximation, almost always true
    {
        return normal * -1;
    }
    return normal;
}

// construct from columns
mat3 lx_ConstructMatrix(vec3 a, vec3 b, vec3 c)
{
    mat3 matrix = mat3(0);
    matrix[0] = a;
    matrix[1] = b;
    matrix[2] = c;
    return matrix;
}

mat4 lx_CreateTransform(vec3 pos, vec3 rot, vec3 scale)
{
    vec3 sr = sin(rot);
    vec3 cr = cos(rot);

    return mat4(
        scale.x * cr.y * cr.z, scale.x * cr.y * sr.z, scale.x * -sr.y, 0.0,
        scale.y * (sr.x * sr.y * cr.z + cr.x * -sr.z), scale.y * (sr.x * sr.y * sr.z + cr.x * cr.z), scale.y * sr.x * cr.y, 0.0,
        scale.z * (cr.x * sr.y * cr.z + -sr.x * -sr.z), scale.z * (cr.x * sr.y * sr.z + -sr.x * cr.z), scale.z * cr.x * cr.y, 0.0,
        pos.x, pos.y, pos.z, 1.0
    );
}