#pragma once

namespace CPPHelper
{
    public ref class ClockUtil sealed
    {
    public:
		ClockUtil();
		static void DelayMilisecond(unsigned int milisecond);
		static void DelayMicrosecond(unsigned int microsecond);
		static unsigned __int64 ConvertToMicrosecond(unsigned __int64 valueQPC);
		static unsigned __int64 ConvertToQPC(unsigned __int64 valuemicroseconds);
		static unsigned __int64 GetCurrentMicrosecond();
	private:
		static LARGE_INTEGER getQueryPerformanceFrequency();

    };
}
