#version luma-dx

uniform lx_Light light;
uniform lx_Material material;

in vec3 fragPos;
in vec3 normal;

uniform vec3 cameraPos;

uniform int ambientLighting;
uniform int diffuseLighting;
uniform int specularLighting;


[quad]
void main()
{
    vec3 norm = normalize(normal);

    vec3 ambient = light.ambient * material.ambient;
    
    vec3 lightDir = normalize(light.position - fragPos);
    
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * material.diffuse;
    
    vec3 viewDir = normalize(cameraPos - fragPos);
    
    //vec3 reflectDir = reflect(-lightDir, normal);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(norm, halfwayDir), 0.0), material.shininess);
    
    vec3 specular = diff * light.specular * spec * material.specular;
    
    // fix buggy lighting
    if (abs(diffuse.x)+abs(diffuse.y)+abs(diffuse.z) == 0){specular*=0;}

    lx_FragColour = lx_Colour( ambient * float(ambientLighting) + diffuse * float(diffuseLighting) + specular * float(specularLighting) );
}

[cube]
void main()
{
    lx_FragColour = vec4(light.diffuse,1.0);
}
