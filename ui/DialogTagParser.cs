using System.Collections.Generic;
using System.Text;

public struct TextSegment
{
    public string Text;
    public float PauseBefore;
    public float CpsOverride;

    public TextSegment(string text, float pauseBefore = 0f, float cpsOverride = 0f)
    {
        Text = text;
        PauseBefore = pauseBefore;
        CpsOverride = cpsOverride;
    }
}

public static class DialogTagParser
{
    public static List<TextSegment> Parse(string input)
    {
        var segments = new List<TextSegment>();
        var text = new StringBuilder();
        float pendingPause = 0f;
        float pendingCps = 0f;

        int i = 0;
        while (i < input.Length)
        {
            if (input[i] == '[')
            {
                int close = input.IndexOf(']', i);
                if (close == -1)
                {
                    GameLogger.Warn("DialogTagParser", $"Unclosed '[' at position {i} — treating as literal");
                    text.Append(input[i]);
                    i++;
                    continue;
                }

                string tag = input.Substring(i + 1, close - i - 1);
                int eq = tag.IndexOf('=');

                if (eq != -1)
                {
                    string tagName = tag.Substring(0, eq).Trim();
                    string tagValue = tag.Substring(eq + 1).Trim();

                    if (tagName == "pause")
                    {
                        if (text.Length > 0)
                        {
                            segments.Add(new TextSegment(text.ToString(), pendingPause, pendingCps));
                            text.Clear();
                            pendingPause = 0f;
                            pendingCps = 0f;
                        }

                        if (float.TryParse(tagValue, out float pauseSec))
                            pendingPause = pauseSec;
                        else
                            GameLogger.Warn("DialogTagParser", $"Invalid [pause=...] value '{tagValue}' — ignoring");
                    }
                    else if (tagName == "speed")
                    {
                        if (text.Length > 0)
                        {
                            segments.Add(new TextSegment(text.ToString(), pendingPause, pendingCps));
                            text.Clear();
                            pendingPause = 0f;
                            pendingCps = 0f;
                        }

                        pendingCps = tagValue switch
                        {
                            "slow" => 20f,
                            "fast" => 80f,
                            "instant" => -1f,
                            _ => 0f
                        };

                        if (pendingCps == 0f && tagValue != "0")
                            GameLogger.Warn("DialogTagParser", $"Unknown [speed=...] value '{tagValue}' — using default");
                    }
                    else
                    {
                        GameLogger.Warn("DialogTagParser", $"Unrecognized tag '[{tagName}=...]' at position {i} — treating as literal");
                        text.Append(input.Substring(i, close - i + 1));
                    }
                }
                else
                {
                    // Bare tag like [b] — pass through as literal
                    text.Append(input.Substring(i, close - i + 1));
                }

                i = close + 1;
            }
            else
            {
                text.Append(input[i]);
                i++;
            }
        }

        if (text.Length > 0)
            segments.Add(new TextSegment(text.ToString(), pendingPause, pendingCps));

        return segments;
    }
}
