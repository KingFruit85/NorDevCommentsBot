using Discord;

namespace NorDevBestOfBot.Extensions;

public static class ColourExtensions
{
    public static List<Color> AllowedColours()
    {
        var colours = new List<Color>
        {
            new(244, 67, 54), // #F44336 (Red)
            new(0, 188, 212), // #00BCD4 (Cyan)
            new(156, 39, 176), // #9C27B0 (Purple)
            new(255, 193, 7), // #FFC107 (Amber)
            new(76, 175, 80), // #4CAF50 (Green)
            new(233, 30, 99), // #E91E63 (Pink)
            new(33, 150, 243), // #2196F3 (Blue)
            new(255, 87, 34), // #FF5722 (Deep Orange)
            new(63, 81, 181), // #3F51B5 (Indigo)
            new(255, 152, 0), // #FF9800 (Orange)
            new(205, 220, 57), // #CDDC39 (Lime)
            new(158, 158, 158), // #9E9E9E (Grey)
            new(255, 235, 59), // #FFEB3B (Yellow)
            new(48, 79, 254), // #304FFE (Blue)
            new(255, 64, 129), // #FF4081 (Pink)
            new(63, 81, 181), // #3F51B5 (Indigo)
            new(33, 150, 243), // #2196F3 (Blue)
            new(255, 87, 34), // #FF5722 (Deep Orange)
            new(255, 152, 0) // #FF9800 (Orange)
        };

        return colours;
    }

    public static Color GetRandomColour()
    {
        var postColours = new List<Color>
        {
            new(244, 67, 54), // #F44336 (Red)
            new(0, 188, 212), // #00BCD4 (Cyan)
            new(156, 39, 176), // #9C27B0 (Purple)
            new(255, 193, 7), // #FFC107 (Amber)
            new(76, 175, 80) // #4CAF50 (Green)
        };

        var rand = new Random();
        var colour = postColours[rand.Next(postColours.Count)];

        return colour;
    }
}