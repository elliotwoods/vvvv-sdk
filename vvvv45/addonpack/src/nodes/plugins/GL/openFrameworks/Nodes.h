#pragma once

#include "ofMain.h"
#include "ofxVVVV.h"

namespace CNodes {
	class ofSetupScreenPerspectiveNode : public ofxVVVV::Node {
	public:
		void draw();
	};

	class ofLineNode : public ofxVVVV::Node {
	public:
		void draw();
	};

	class ofDrawBitmapStringNode : public ofxVVVV::Node {
	public:
		void draw();
	};

	class GraphicsExampleNode : public ofxVVVV::Node {
	public:
		void setup();
		void update();
		void draw();

		void keyPressed(int key);
		void mouseMoved( int x, int y );

		float 	counter;
		bool	bSmooth;

		deque<ofPoint> cursorHistory;
	};
}