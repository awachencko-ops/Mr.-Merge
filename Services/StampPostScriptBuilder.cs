using System.Globalization;

namespace MrMergePdfStamper.Services;

public static class StampPostScriptBuilder
{
    public static string Generate(int spreadNumber, double rightOffsetMm, double topOffsetMm, string fontName, double fontSize)
    {
        const double pointsPerMm = 72.0 / 25.4;
        var rightOffsetPt = rightOffsetMm * pointsPerMm;
        var topOffsetPt = topOffsetMm * pointsPerMm;

        return string.Format(
            CultureInfo.InvariantCulture,
            "<< /EndPage {{ 0 eq {{ pop gsave " +
            "/{0} findfont {1} scalefont setfont " +
            "currentpagedevice /PageSize get aload pop " +
            "/PageHeight exch def /PageWidth exch def " +
            "({2}) stringwidth pop /TW exch def " +
            "PageWidth {3} sub TW sub " +
            "PageHeight {4} sub moveto " +
            "0 0 0 setrgbcolor " +
            "({2}) show grestore true }} " +
            "{{ pop false }} ifelse }} bind >> setpagedevice",
            fontName,
            fontSize,
            spreadNumber,
            rightOffsetPt,
            topOffsetPt + fontSize);
    }
}
