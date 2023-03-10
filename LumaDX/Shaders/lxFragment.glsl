out vec4 lx_FragColour;

uniform bool lx_IsGammaCorrectionEnabled;

vec4 lx_GammaCorrect(vec4 colour, float gamma)
{
    return vec4(pow(colour.rgb, vec3(gamma)),colour.w);
}
vec3 lx_GammaCorrect(vec3 colour, float gamma)
{
    return pow(colour, vec3(gamma));
}

float lx_GammaCorrect(float value, float gamma)
{
    return pow(value, gamma);
}

float lx_Diffuse(in vec3 normal, in vec3 fragPos, in vec3 lightPos)
{
    return max(dot(normal, lightPos - fragPos), 0.0);
}

vec3 lx_BasePhong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in vec2 texCoords, in vec2 specTexCoords, in int textureMode, in lx_Material material, in lx_Light light, float lightMult)
{
    normal = normalize(normal);

    vec3 baseTexSample = vec3(1.0);
    vec3 specTexSample = vec3(1.0);

    if (textureMode > 0)
    {
        baseTexSample = vec3(texture(material.baseTex, texCoords));
    }
    
    // possibly temporary? removes really buggy lighting when completely black
    if (dot(baseTexSample,baseTexSample) == 0.0){
        baseTexSample += vec3(0.01);
    }

    if (textureMode > 1)
    {
        specTexSample = vec3(texture(material.specTex, specTexCoords));
    }
    
    if (lx_IsGammaCorrectionEnabled)
    {
        baseTexSample = lx_GammaCorrect(baseTexSample,2.2);
        specTexSample = lx_GammaCorrect(specTexSample,2.2);
    }

    vec3 ambient = light.ambient * material.ambient * baseTexSample;
    
    vec3 lightDir = vec3(0.0);
    
    if (light.cutOff > 0)
    {
        lightDir = light.position - fragPos;
    }
    else
    {
        lightDir = -light.direction;
    }
    
    float distance = length(lightDir);
    float attenuation = 1.0 / dot(light.attenuation,vec3(1.0,distance,distance*distance));
    
    lightDir = lightDir/distance;
    
    float intensity = 1.0;
    
    if (light.cutOff < 1 && light.cutOff > 0) // if this light is a spotlight
    {
        float cosTheta = dot(lightDir, normalize(-light.direction));
        
        if (light.outerCutOff > 0) // fade out at edge of splotlight
        {
            intensity = clamp((cosTheta - light.outerCutOff) / (light.cutOff - light.outerCutOff), 0.0, 1.0);
        }
        
        if(cosTheta < light.outerCutOff) // if angle > cutOff, but since it's cosine(angle), we use less than (cosine graph initially decreases)
        {
            return ambient;
        }
    }
    
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * material.diffuse * baseTexSample;
    
    vec3 viewDir = normalize(cameraPos - fragPos);
    
    //vec3 reflectDir = reflect(-lightDir, normal);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);
    
    vec3 specular = diff * light.specular * spec * material.specular * specTexSample;
    
    // fix buggy lighting
    if (abs(diffuse.x)+abs(diffuse.y)+abs(diffuse.z) == 0){specular*=0;}

    ambient  *= attenuation; 
    diffuse  *= attenuation * intensity;
    specular *= attenuation * intensity;

    return ambient + lightMult * (diffuse + specular); 
}

vec3 lx_Phong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in lx_Material material, in lx_Light light, float lightMult)
{
    return lx_BasePhong(normal,fragPos,cameraPos,vec2(0.0),vec2(0.0),0,material,light,lightMult);
}
vec3 lx_Phong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in vec2 texCoords, in lx_Material material, in lx_Light light, float lightMult)
{
    return lx_BasePhong(normal,fragPos,cameraPos,texCoords,vec2(0.0),1,material,light,lightMult);
}
vec3 lx_Phong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in vec2 texCoords, in vec2 specTexCoords, in lx_Material material, in lx_Light light, float lightMult)
{
    return lx_BasePhong(normal,fragPos,cameraPos,texCoords,specTexCoords,2,material,light,lightMult);
}


vec3 lx_DeferredPhong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in vec3 albedoInput, in float specularInput, in lx_Material material, in lx_Light light, float lightMult)
{
    normal = normalize(normal);

    vec3 baseTexSample = albedoInput;
    vec3 specTexSample = vec3(specularInput);

    
    if (lx_IsGammaCorrectionEnabled)
    {
        baseTexSample = lx_GammaCorrect(baseTexSample,2.2);
        specTexSample = lx_GammaCorrect(specTexSample,2.2);
    }

    vec3 ambient = light.ambient * material.ambient * baseTexSample;
    
    vec3 lightDir = vec3(0.0);
    
    if (light.cutOff > 0)
    {
        lightDir = light.position - fragPos;
    }
    else
    {
        lightDir = -light.direction;
    }
    
    float distance = length(lightDir);
    float attenuation = 1.0 / dot(light.attenuation,vec3(1.0,distance,distance*distance));
    
    lightDir = lightDir/distance;
    
    float intensity = 1.0;
    
    if (light.cutOff < 1 && light.cutOff > 0) // if this light is a spotlight
    {
        float cosTheta = dot(lightDir, normalize(-light.direction));
        
        if (light.outerCutOff > 0) // fade out at edge of splotlight
        {
            intensity = clamp((cosTheta - light.outerCutOff) / (light.cutOff - light.outerCutOff), 0.0, 1.0);
        }
        
        if(cosTheta < light.outerCutOff) // if angle > cutOff, but since it's cosine(angle), we use less than (cosine graph initially decreases)
        {
            return ambient;
        }
    }
    
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * material.diffuse * baseTexSample;
    
    vec3 viewDir = normalize(cameraPos - fragPos);
    
    //vec3 reflectDir = reflect(-lightDir, normal);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);
    
    vec3 specular = diff * light.specular * spec * material.specular * specTexSample;
    
    // fix buggy lighting
    if (abs(diffuse.x)+abs(diffuse.y)+abs(diffuse.z) == 0){specular*=0;}

    ambient  *= attenuation; 
    diffuse  *= attenuation * intensity;
    specular *= attenuation * intensity;

    return ambient + lightMult * (diffuse + specular); 
}


vec3 lx_DeferredPhong(in vec3 normal, in vec3 fragPos, in vec3 cameraPos, in vec3 albedoInput, in float specularInput, in lx_Material material, in lx_Light lights[NUM_LIGHTS], in int lightCount, mat3 tbn, float lightMult)
{
    normal = normalize(normal);

    vec3 baseTexSample = albedoInput;
    vec3 specTexSample = vec3(specularInput);

    
    if (lx_IsGammaCorrectionEnabled)
    {
        baseTexSample = lx_GammaCorrect(baseTexSample,2.2);
        specTexSample = lx_GammaCorrect(specTexSample,2.2);
    }

    vec3 ambient;
    
    vec3 lightDir = vec3(0.0);
    
    float distance;
    float attenuation;
     
    float intensity = 1.0;
    float diff;
    vec3 diffuse;
    vec3 viewDir;
    
    vec3 halfwayDir;
    float spec;
    vec3 specular;
    
    lx_Light light;
    vec3 universalAmbient = material.ambient * baseTexSample;
    vec3 outColour = vec3 (0.0);
    
    for(int i = 0; i < lightCount; i++)
    {
        light = lx_MoveLight(lights[i],tbn*lights[i].position);
        
        ambient = light.ambient;
    
        if (light.cutOff > 0)
        {
            lightDir = light.position - fragPos;
        }
        else
        {
            lightDir = -light.direction;
        }
        
        distance = length(lightDir);
        attenuation = 1.0 / dot(light.attenuation,vec3(1.0,distance,distance*distance));
        
        lightDir = lightDir/distance;
        
        if (light.cutOff < 1 && light.cutOff > 0) // if this light is a spotlight
        {
            float cosTheta = dot(lightDir, normalize(-light.direction));
            
            if (light.outerCutOff > 0) // fade out at edge of splotlight
            {
                intensity = clamp((cosTheta - light.outerCutOff) / (light.cutOff - light.outerCutOff), 0.0, 1.0);
            }
            
            if(cosTheta < light.outerCutOff) // if angle > cutOff, but since it's cosine(angle), we use less than (cosine graph initially decreases)
            {
                continue;//return ambient;
            }
        }
        
        diff = max(dot(normal, lightDir), 0.0);
        diffuse = light.diffuse * diff * material.diffuse * baseTexSample;
        
        viewDir = normalize(cameraPos - fragPos);
        
        halfwayDir = normalize(lightDir + viewDir);
        spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);
        
        specular = diff * light.specular * spec * material.specular * specTexSample * light.diffuse;
        
        // fix buggy lighting
        if (abs(diffuse.x)+abs(diffuse.y)+abs(diffuse.z) == 0){specular*=0;}

        diffuse  *= attenuation * intensity * ambient;
        specular *= attenuation * intensity * ambient;
        
        outColour += lightMult * (diffuse + specular);
    }

    return universalAmbient + outColour; 
}

vec3 lx_SampleOffsetDirections[20] = vec3[]
(
   vec3( 1,  1,  1), vec3( 1, -1,  1), vec3(-1, -1,  1), vec3(-1,  1,  1), 
   vec3( 1,  1, -1), vec3( 1, -1, -1), vec3(-1, -1, -1), vec3(-1,  1, -1),
   vec3( 1,  1,  0), vec3( 1, -1,  0), vec3(-1, -1,  0), vec3(-1,  1,  0),
   vec3( 1,  0,  1), vec3(-1,  0,  1), vec3( 1,  0, -1), vec3(-1,  0, -1),
   vec3( 0,  1,  1), vec3( 0, -1,  1), vec3( 0, -1, -1), vec3( 0,  1, -1)
);   

float lx_ShadowCalculation(samplerCube depthMap, vec3 fragPos, vec3 lightPos, vec3 cameraPos, float farPlane)
{
    vec3 fragToLight = fragPos - lightPos;
    float closestDepth = texture(depthMap, fragToLight).r;
    closestDepth *= farPlane;
    float currentDepth = length(fragToLight);
    
    float bias = 0.15;
    float shadow = currentDepth - bias > closestDepth ? 0.0 : 1.0;
    
    
    if (shadow == 0.0)
    {
        int samples = 20;
        float viewDistance = length(cameraPos - fragPos);
        float diskRadius = (1.0 + (viewDistance / farPlane)) / 30.0; 
        for(int i = 0; i < samples; ++i)
        {
            closestDepth = texture(depthMap, fragToLight + lx_SampleOffsetDirections[i] * diskRadius).r;
            closestDepth *= farPlane;
            if(currentDepth - bias < closestDepth)
                shadow += 1.0;
        }
        shadow /= float(samples);
    
    }

    
    return shadow;
}


float lx_ShadowCalculation(sampler2D shadowTexture, vec4 fragPosLightSpace)
{
    vec3 projCoords = (fragPosLightSpace.xyz / fragPosLightSpace.w) * 0.5 + 0.5;
    float closestDepth = texture(shadowTexture, projCoords.xy).r;
    float currentDepth = projCoords.z;
    float textureDepth = texture(shadowTexture, projCoords.xy).r;
    
    float shadow = (currentDepth > textureDepth ? 1.0 : 0.0);

    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return 1.0-shadow;
}

float lx_ShadowCalculation(sampler2D shadowTexture, vec4 fragPosLightSpace, float texelOffset, int range, float divisor)
{
    vec3 projCoords = (fragPosLightSpace.xyz / fragPosLightSpace.w) * 0.5 + 0.5;
    float closestDepth = texture(shadowTexture, projCoords.xy).r;
    float currentDepth = projCoords.z;
    float textureDepth = texture(shadowTexture, projCoords.xy).r;
    
    float shadow = (currentDepth > textureDepth ? 1.0 : 0.0);
    

    if (shadow == 0.0)
    {
        vec2 texelSize = texelOffset / textureSize(shadowTexture, 0);
        for(int x = -range; x <= range; x++)
        {
            for(int y = -range; y <= range; y++)
            {
                float pcfDepth = texture(shadowTexture, projCoords.xy + vec2(x, y) * texelSize).r; 
                shadow += currentDepth > pcfDepth ? 1.0 : 0.0;
            }    
        }
        shadow = min(shadow/divisor,0.98);
    }
    
    if(projCoords.z > 1.0)
        shadow = 0.0;
        
        
    return 1.0-shadow;
}

vec4 lx_MultiSample(sampler2DMS sampler, ivec2 texCoords, int numSamples)
{
    vec4 pixelColour = texelFetch(sampler, texCoords, 0);
    for(int i = 1; i < numSamples; i++)
    {
        pixelColour += texelFetch(sampler, texCoords, i);
    }
    
    return pixelColour / float(numSamples);
}


vec3 lx_NormalMap(sampler2D normalMap, vec2 texCoords)
{
    return normalize(texture(normalMap, texCoords).rgb * 2.0 - 1.0);
}

// just for readability on longer lines
vec4 lx_Colour(vec3 colour)
{
    return vec4(colour,1.0);
}
vec4 lx_Colour(vec2 colour)
{
    return vec4(colour,1.0,1.0);
}
vec4 lx_Colour(float colour)
{
    return vec4(colour);
}

vec2 lx_ParallaxMapping(sampler2D depthMap, vec2 texCoords, vec3 viewDir, float heightScale, float minLayers, float maxLayers)
{ 
    // size of each layer
    float layerDepth = 1.0 / mix(maxLayers, minLayers, max(dot(vec3(0.0, 0.0, 1.0), viewDir), 0.0));
    
    // amount to shift the texture coordinates per layer
    vec2 P = viewDir.xy * heightScale; 
    vec2 deltaTexCoords = P * layerDepth;


    vec2  currentTexCoords = texCoords;
    float currentDepthMapValue = texture(depthMap, currentTexCoords).r;
    float currentLayerDepth = 0.0;
      
    while(currentLayerDepth < currentDepthMapValue)
    {
        // move texCoords with parallaxing
        currentTexCoords -= deltaTexCoords; 
        currentDepthMapValue = texture(depthMap, currentTexCoords).r; // sadly this sample step makes optimising layers a lot harder

        // keep incrementing (stops once it hits the imaginary parallaxed object's depth)
        currentLayerDepth += layerDepth;  
    }
    
    // undo last step
    vec2 prevTexCoords = currentTexCoords + deltaTexCoords;

    // get depth after and before collision for linear interpolation
    float afterDepth  = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = texture(depthMap, prevTexCoords).r - currentLayerDepth + layerDepth;
     
    // interpolation of texture coordinates
    float weight = afterDepth / (afterDepth - beforeDepth);
    vec2 finalTexCoords = prevTexCoords * weight + currentTexCoords * (1.0 - weight);
    
    return finalTexCoords;  
}

vec3 lx_ApplyHDR(vec3 colour, float exposure, float gamma)
{
    // exposure tone mapping
    vec3 mapped = vec3(1.0) - exp(-colour * exposure);
    if (gamma != 1.0)
    {
        // gamma correction 
        mapped = pow(mapped, vec3(1.0 / gamma));
    }
    
    return mapped;
}

void lx_DiscardBackground(vec4 fragColour)
{
    if (fragColour.a == 0.0) // only way this is possible is if fragColour was not written to and therefore this shouldn't be rendered
    {
        discard;
    }
}

[main]
lx_FragColour = vec4(0.0);

[post-main]
if (lx_IsGammaCorrectionEnabled)
{
    lx_FragColour = lx_GammaCorrect( lx_FragColour , 1.0/2.2);
}
