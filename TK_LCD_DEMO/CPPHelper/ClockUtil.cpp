#include "pch.h"
#include "ClockUtil.h"

using namespace CPPHelper;
using namespace Platform;

ClockUtil::ClockUtil()
{
}

void CPPHelper::ClockUtil::DelayMilisecond(unsigned int milisecond)
{
	//Sleep(milisecond);
	DelayMicrosecond(milisecond * 1000);
}

void CPPHelper::ClockUtil::DelayMicrosecond(unsigned int microsecond)
{
	LARGE_INTEGER start, stop, tmp;
	QueryPerformanceCounter(&start);
	tmp.QuadPart = microsecond;
	stop.QuadPart = start.QuadPart + ConvertToQPC(tmp.QuadPart);
	do {
		QueryPerformanceCounter(&tmp);
	} while (tmp.QuadPart < stop.QuadPart);
}

//See:
//https://msdn.microsoft.com/en-us/library/windows/desktop/dn553408(v=vs.85).aspx
unsigned __int64 CPPHelper::ClockUtil::ConvertToMicrosecond(unsigned __int64 valueQPC)
{
	auto qpf = getQueryPerformanceFrequency();
	unsigned __int64 result = valueQPC * 1000000;
	result /= qpf.QuadPart;
	return result;
}

unsigned __int64 CPPHelper::ClockUtil::ConvertToQPC(unsigned __int64 valuemicroseconds)
{
	auto qpf = getQueryPerformanceFrequency();
	unsigned __int64 result = valuemicroseconds * qpf.QuadPart;
	result /= 1000000;
	return result;
}

unsigned __int64 CPPHelper::ClockUtil::GetCurrentMicrosecond()
{
	LARGE_INTEGER start;
	QueryPerformanceCounter(&start);
	return ConvertToMicrosecond(start.QuadPart);
}

LARGE_INTEGER CPPHelper::ClockUtil::getQueryPerformanceFrequency()
{
	static bool run = false;
	static LARGE_INTEGER _qpf;
	if (!run) {
		run = true;
		QueryPerformanceFrequency(&_qpf);
	}
	return _qpf;
}
