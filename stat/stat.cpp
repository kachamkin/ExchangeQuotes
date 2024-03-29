﻿#include "header.h"

int port;
string groupAddress;
int medianeInterval;
int modeStep;

int64_t messagesCount = 0;
int64_t lostMessagesCount = 0;
double average = 0;
double deviationSum = 0;
double mediane = 0;
double mode = 0;

bool drawChart = false;
bool exitApp = false;
extern map<int64_t, int64_t> dt;
extern int64_t maxVal;

boost::signals2::signal<void(char*)> dataReceived;

bool ReadXML(string path)
{
	boost::property_tree::ptree propertyTree;

	try
	{
		ifstream settings(path + "/Settings.xml");
		boost::property_tree::read_xml(settings, propertyTree);
		settings.close();

		for (const auto& v : propertyTree.get_child("settings"))
		{
			string first = v.first;
			string second = v.second.data();

			if (first == "GroupAddress")
				groupAddress = second;
			else if (first == "Port")
				port = atoi(second.data());
			else if (first == "MedianeInterval")
				medianeInterval = atoi(second.data());
			else if (first == "ModeStep")
				modeStep = atoi(second.data());
		}
		return true;
	}
	catch (...)
	{
		return false;
	}
}

int GetConsoleBufferWidth(HANDLE hOut)
{
	CONSOLE_SCREEN_BUFFER_INFO bi{};
	return GetConsoleScreenBufferInfo(hOut, &bi) ?
		bi.dwSize.X : 0;
}

void Print()
{
	drawChart = false;

	HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
	if (!hOut)
		return;

	double bufferWidth = GetConsoleBufferWidth(hOut);
	if (!bufferWidth)
		return;
	double max = bufferWidth / (maxVal + 168.0);

	cout << endl;
	for (int i = 0; i < bufferWidth / 2 - 16; i++)
		cout << " ";
	cout << "Packets ";
	cout << messagesCount - medianeInterval;
	cout << " - ";
	cout << messagesCount;
	cout << "\n\n";

	for (const auto& item : dt)
	{
		cout << item.first;
		for (int i = 0; i < 8 - to_string(item.first).length(); i++)
			cout << " ";

		SetConsoleTextAttribute(hOut, DARK_GREY_BACKGROUND);
		for (double i = 0; i < item.second * max; i++)
			cout << " ";
		SetConsoleTextAttribute(hOut, BLACK_BACKGROUND);
		cout << " ";
		cout << item.second;
		cout << endl;
	}

	cout << endl;
	for (int i = 0; i < 60; i++)
		cout << '*';
	cout << endl;
}

void Output()
{
	while (true)
	{
		int c = getchar();
		if (c == '\n')
		{
			cout << "\nTotal messages received: ";
			cout << messagesCount << endl;
			cout << "Total messages \"lost\":   ";
			cout << lostMessagesCount << endl;
			cout << "Average:                 ";
			cout << average << endl;
			cout << "Standard deviation:      ";
			cout << sqrt(deviationSum / (messagesCount + 1)) << endl;
			cout << "Mediane:                 ";
			cout << mediane << endl;
			cout << "Mode:                    ";
			cout << mode << endl;
			cout << endl;
			for (int i = 0; i < 60; i++)
				cout << '*';
			cout << endl;
		}
		else if (c == 'q')
		{
			exitApp = true;
			this_thread::sleep_for(chrono::milliseconds(1));
			exit(0);
		}
		else if (c == 'p')
			drawChart = true;
	}
}

int main(int argc, char* argv[])
{
	filesystem::path p = filesystem::path(argv[0]).parent_path();
	if (!ReadXML(filesystem::path(argv[0]).parent_path().string()))
		cout << "Failed to read settings!\n";

	cout << "\n================== Stat params calculation ==================\n\n";
	cout << "Please press:" << endl;
	cout << "	- \"Enter\" for text data output" << endl;
	cout << "	- \"P + Enter\" for chart output" << endl;
	cout << "	- \"Q + Enter\" for quit" << endl;

	dataReceived.connect(UpdateData);
	thread(Output).detach();
	
	Listen();
}