#version 330 core

uniform sampler2D texture0;
in vec2 texCoords;

out vec4 fragColour;

void main()
{
    vec3 sample = texture(texture0, texCoords).rgb;
    float value = (sample.r + sample.g + sample.b) / 3.0;
    if (value > 0.63 && value < 0.86) {value = 0.86;}
    value += 0.235;
    fragColour = vec4(value,value,value,1.0);
}