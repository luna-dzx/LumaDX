#version luma-dx

uniform sampler2D sampler;
uniform sampler2D brightSample;
uniform sampler2D occlusionSample;

in vec2 texCoords;

uniform float exposure;

uniform int aoEnabled;
uniform int visualizeAO;

uniform float weight[5] = float[] (0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

void main()
{
    if (visualizeAO == 1)
    {
        lx_FragColour = texture(occlusionSample, texCoords);
    }
    else
    {
        vec4 mainSample = texture(sampler, texCoords);
        vec4 bloomSample = texture(brightSample, texCoords);
        if (bloomSample.a < 1.0/256.0) discard;
        
        float occlusion = 1.0;
        if (aoEnabled == 1) occlusion = texture(occlusionSample, texCoords).r;
        
        lx_FragColour = vec4(lx_ApplyHDR(mainSample.rgb + bloomSample.rgb,exposure,2.2)*occlusion, bloomSample.a);
    }
}