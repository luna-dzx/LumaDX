#version luma-dx

uniform sampler2D texture0;
uniform vec2 screenSize;

in vec2 texCoords;

void main()
{
    lx_FragColour = texture(texture0,texCoords * vec2(screenSize.x/screenSize.y,1.0));
}