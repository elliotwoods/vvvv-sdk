// oF-vvvv.h

#pragma once

#include "Nodes.h"
#include "ofxVVVV.h"
#include "glut.h"
#include <set>

using namespace System;
using namespace VVVV::PluginInterfaces::V1;
using namespace VVVV::Utils::VColor;
using namespace VVVV::Utils::VMath;
using namespace VVVV::Nodes::OpenGL;

namespace VVVV {
	namespace Nodes {
		namespace OpenFrameworks {

			public ref class WrappedApp
			{
			public:
				WrappedApp(string name, ofxVVVV::Node *);
				virtual ~WrappedApp();

				void setup();
				void update();
				void draw();
				
				void keyPressed(int key);
				void keyReleased(int key);
				void mouseMoved(int x, int y);
				void mouseDragged(int x, int y, int button);
				void mousePressed(int x, int y, int button);
				void mouseReleased(int x, int y, int button);

			protected:
				IPluginHost ^ FHost;
				ofxVVVV::Node * node;
				String^ name;
				bool isSetup;
			private:
				//static std::set<ofPtr<ofAppBaseWindow> *> preparedWindows;
			};
	
			public ref class ofSetupScreenPerspectiveNode : public WrappedApp {
			public:
				ofSetupScreenPerspectiveNode() : 
					WrappedApp("ofSetupScreenPersective", new CNodes::ofSetupScreenPerspectiveNode()) { };
			};

			public ref class ofLineNode : public WrappedApp {
			public:
				ofLineNode() : 
					WrappedApp("ofLine", new CNodes::ofLineNode()) { };
			};

			public ref class ofDrawBitmapStringNode : public WrappedApp {
			public:
				ofDrawBitmapStringNode() :
					WrappedApp("ofDrawBitmapString", new CNodes::ofDrawBitmapStringNode()) { };
			};

			public ref class GraphicsExampleNode : public WrappedApp {
			public:
				GraphicsExampleNode() :
					WrappedApp("graphicsExample", new CNodes::GraphicsExampleNode()) { };
			};
		}
	}
}
