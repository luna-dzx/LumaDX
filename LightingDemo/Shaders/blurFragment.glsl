#version luma-dx

uniform sampler2D texture0;
in vec2 texCoords;

void main() {
    vec2 texelSize = 1.0 / vec2(textureSize(texture0, 0));
    float result = 0.0;
    for (int x = -2; x < 2; ++x) 
    {
        for (int y = -2; y < 2; ++y) 
        {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(texture0, texCoords + offset).r;
        }
    }
    lx_FragColour = vec4(result / (4.0 * 4.0));
}  