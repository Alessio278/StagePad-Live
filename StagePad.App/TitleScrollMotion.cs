namespace StagePad.App;
internal static class TitleScrollMotion
{
    public static double Offset(double seconds,double textWidth,double viewportWidth)
    {
        double distance=Math.Max(0,textWidth-viewportWidth);
        if(distance<=1 || viewportWidth<=0)return 0;
        const double pause=1.2,speed=28;
        double travel=distance/speed;
        double phase=Math.Max(0,seconds)%(pause*2+travel*2);
        if(phase<pause)return 0;
        phase-=pause;
        if(phase<travel)return -phase*speed;
        phase-=travel;
        if(phase<pause)return -distance;
        phase-=pause;
        return -distance+phase*speed;
    }
}
