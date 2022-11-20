#include "header.h"

extern int medianeInterval;
extern int modeStep;
extern int64_t messagesCount;
extern int64_t lostMessagesCount;
extern double average;
extern double deviationSum;
extern double mediane;
extern double mode;

int64_t maxKey;
int64_t maxVal;
int64_t medKey;
int64_t sumF;

double stdDev = 0;
int64_t sum = 0;
int64_t initMessageNumber = -1;
map<int64_t, int64_t> dt;
extern bool drawChart;

extern boost::signals2::signal<void(char*)> dataReceived;

void GetMedMax()
{
    bool found = false;
    sumF = 0;
    int half = medianeInterval / 2;
    for (const auto& p : dt)
    {
        if (p == *dt.begin() || p.second > maxVal)
        {
            maxVal = p.second;
            maxKey = p.first;
        }

        sumF += p.second;
        if (sumF >= half && !found)
        {
            medKey = p.first;
            found = true;
        }
    }
}

double GetMode()
{
    if (dt.at(maxKey) > 1)
    {
        int64_t fm0_1 = 0;
        auto p = dt.find(medKey);
        p--;
        if (p != dt.end())
            fm0_1 = p->second;

        int64_t fm01 = 0;
        p++;
        p++;
        if (p != dt.end())
            fm01 = p->second;

        return maxKey - 0.5 * modeStep + modeStep * (maxVal - fm0_1) / (2.0 * maxVal - fm0_1 - fm01);
    }
    return 0;
}

double GetMediane()
{
    int64_t fm0_1 = 0;
    auto p = dt.find(medKey);
    p--;
    if (p != dt.end())
        fm0_1 = p->second;

    return medKey - 0.5 * modeStep + modeStep * (0.5 * sumF - fm0_1) / dt.at(medKey);
}

void UpdateTable(int64_t value)
{
    if (dt.contains(value))
        dt[value]++;
    else
        dt.insert({ value, 1 });
}

void UpdateData(char* pData)
{
    messagesCount++;

    int64_t num;
    memcpy(&num, pData, 8);
    if (initMessageNumber == -1)
        initMessageNumber = num - 1;
    lostMessagesCount = num - initMessageNumber - messagesCount;

    memcpy(&num, pData + 8, 8);

    sum += num;
    average = (double)sum / messagesCount;

    double deviation = (double)num - average;
    deviationSum += deviation * deviation;

    stdDev = sqrt(deviationSum / (messagesCount + 1.));

    UpdateTable(((num % modeStep >= modeStep / 2 ? num + modeStep : num) / modeStep) * modeStep);

    if (messagesCount >= medianeInterval && messagesCount % medianeInterval == 0)
    {
        GetMedMax();
        int64_t diff = messagesCount - medianeInterval;
        mediane = (diff * mediane + medianeInterval * GetMediane()) / messagesCount;
        mode = (diff * mode + medianeInterval * GetMode()) / messagesCount;
        if (drawChart)
            Print();
        dt.clear();
    }
}