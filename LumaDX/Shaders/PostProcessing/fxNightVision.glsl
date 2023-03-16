// -------------------- fxNightVision.glsl -------------------- //

#version 330 core

uniform sampler2D texture0;
in vec2 texCoords;
out vec4 fragColour;
uniform float time;
uniform float noiseStrength = 0.15;
uniform vec3 goggles = vec3(0.264, 0.394, 18.0);
uniform vec2 screenSize = vec2(1600.0,900.0);

// standard hashing noise function
float noise(vec2 fragCoord, uint seed)
{
    // increase size of pixels
    vec2 coord = floor((fragCoord) * 0.4);
    
    // hash coord to get pseudo random colour
    uvec3 v = uvec3(coord, seed);

    v = v * 1664525u + 1013904223u;

    v.x += v.y*v.z;
    v.y += v.z*v.x;
    v.z += v.x*v.y;

    v ^= v >> 16u;

    v.x += v.y*v.z;
    v.y += v.z*v.x;
    v.z += v.x*v.y;

    // convert hashed integer to 3 floats between 0 and 1
    vec3 vec = vec3(v) * (1.0/float(0xffffffffu));

    // make colour exponential and average 
    vec3 ex = (exp(vec)- 1.0)/3.2;
    return 1.0 - clamp((ex.x + ex.y + ex.z) / (5.0 * (1.0 - noiseStrength)), 0.0 ,1.0);
}


vec3 green_filter(float value)
{
    float saturation = 1.0;
    
    // adjust value to be more exponential
    value = 2.0 * (3.0 - value * 2.0) * value * value;
    if (value > 1.0)
    {   // this is to go from black to green to white
        saturation = 2.0 - value;
        value = 1.0;
    }
    
    float c = value * saturation;
    vec3 m = vec3(value - c);
    return vec3(0,c,0) + m;
}

float get_goggles_value(vec2 uv)
{
    return 1.0 - clamp( (min((uv.x-goggles.y)*(uv.x-goggles.y), (uv.x+goggles.y)*(uv.x+goggles.y))+ uv.y*uv.y - goggles.x) * goggles.z ,0.0,1.0);
}

void main()
{
    vec3 sample = texture(texture0, texCoords).rgb;
    float value = (sample.r + sample.g + sample.b) / 3.0;
    
    // draw black area around circles with blur for goggles effect
    vec2 uv = (gl_FragCoord.xy - (0.5 * screenSize.xy)) / (0.5 * screenSize.x);
    vec2 offset = 10.0 / screenSize.xy;
    float gogglesValue = 0.0;
    for (float i = -4.0; i <= 4.0; i+= 2.0){for (float j = -4.0; j <= 4.0; j+= 2.0){
            gogglesValue += get_goggles_value(uv + offset * vec2(i,0.0)) / 25.0;
    }}
    
    fragColour = vec4((green_filter(value) * noise(gl_FragCoord.xy, uint(time) * 7357u)) * gogglesValue,1.0);
}