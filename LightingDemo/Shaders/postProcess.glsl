#version luma-dx

uniform sampler2D sampler;
uniform sampler2D brightSample;
in vec2 texCoords;

uniform float exposure;

uniform int blurDirection; // 0 for horizontal, 1 for vertical

uniform float weight[5] = float[] (0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

void main()
{
    vec4 mainSample = texture(sampler, texCoords);
    vec4 bloomSample = texture(brightSample, texCoords);
    if (bloomSample.a < 1.0/256.0) discard;
    
    
    lx_FragColour = vec4(lx_ApplyHDR(mainSample.rgb+bloomSample.rgb,exposure,2.2), bloomSample.a);
    
    // here we can freely use vec4(...,1.0); since this is a framebuffer
    // over the whole screen which will never use transparency / alpha

}