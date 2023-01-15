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