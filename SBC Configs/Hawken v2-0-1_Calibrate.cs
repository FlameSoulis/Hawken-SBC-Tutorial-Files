/*
	Hawken v2.0.1 Controls for Steel Battalion
	
	NOTE: 	DO NOT USE 2.1 RC! These settings are for 2.0.1 of the program that
			handles the controller! The value ranges are different and this
			makes it difficult to include a setting to allow different maths
			to be applied. Until this is managed or is upgraded to handle that,
			please use Steel-Battalion-64_2.0.1 instead.
*/
/*
From GameEngine.ini

.Bindings=(Name="XboxTypeS_Back",Command="GBA_ShowScores")
.Bindings=(Name="XboxTypeS_Start",Command="GBA_ShowMenu")

.Bindings=(Name="XboxTypeS_LeftX",Command="GBA_StrafeLeft_Gamepad")
.Bindings=(Name="XboxTypeS_LeftY",Command="GBA_MoveForward_Gamepad")
.Bindings=(Name="XboxTypeS_RightX",Command="GBA_TurnLeft_Gamepad")
.Bindings=(Name="XboxTypeS_RightY",Command="GBA_Look_Gamepad")

.Bindings=(Name="XboxTypeS_LeftTrigger",Command="GBA_Fire")
.Bindings=(Name="XboxTypeS_RightTrigger",Command="GBA_AltFire")

.Bindings=(Name="XboxTypeS_LeftShoulder",Command="GBA_Jump_Gamepad")
.Bindings=(Name="XboxTypeS_RightShoulder",Command="GBA_Boost")

.Bindings=(Name="XboxTypeS_A",Command="GBA_Ability")
.Bindings=(Name="XboxTypeS_B",Command="GBA_Heal | Back")
.Bindings=(Name="XboxTypeS_Y",Command="GBA_Action")
.Bindings=(Name="XboxTypeS_X",Command="GBA_Use | Confirm")
.Bindings=(Name="XboxTypeS_LeftThumbstick",Command="GBA_EnemySpotted")
.Bindings=(Name="XboxTypeS_RightThumbstick",Command="GBA_AltFire2")

.Bindings=(Name="XboxTypeS_DPad_Up",Command="GBA_NeedAssistance")
.Bindings=(Name="XboxTypeS_DPad_Down",Command="GBA_ToggleMap")
.Bindings=(Name="XboxTypeS_DPad_Left",Command="GBA_PrevItem")
.Bindings=(Name="XboxTypeS_DPad_Right",Command="GBA_NextItem")
;.Bindings=(Name="SIXAXIS_AccelX",Command="GBA_TurnLeft_Gamepad")
;.Bindings=(Name="SIXAXIS_AccelZ",Command="GBA_Look_Gamepad")
*/

/*
	Aiming Lever Y		= Xbox Right Y
	Aiming Lever X		= Xbox Right X
	Rotation Lever		= Shift + Xbox Left X
	Sight Change Pressed= B01
	Sight Change X		= Xbox Left X
	Sight Change Y		= 
	Lock On				= B06
	Main Weapon			= B10
	Sub Weapon			= B09
	Gas Pedal			= Xbox Left Y (Pos)
	Brake Pedal			= Xbox Left Y (Neg)
	Clutch Pedal		= B07
	Main Weapon Ctrl	= 1
	Sub Weapon Ctrl		= 2
	Magazine Change		= 3
	Oxygen Toggle		= B02
	Filtration Toggle	= B11
	
	
	Buttons
	01	= A
	02	= B
	03	= X
	04	= Y
	05	= Left Stick
	06	= Right Stick
	07	= Left Shoulder
	08	= Right Shoulder
	09	= Left Trigger
	10	= Right Trigger
	11	= Back Button
	12	= Start Button
*/

using SBC;
using System;
namespace SBC {
	public class DynamicClass {
		SteelBattalionController controller;
		vJoy joystick;
		bool acquired;
		const int refreshRate 			= 30;//number of milliseconds between call to mainLoop
		
		const int iRotationLeverDead	= 32;
		const int iLeftPedalDead		= 32;
		const int iMovementPedalBoost	= 196;
		
		const int iGlowyGoodnessMin		= 1; //0-15 for glow effect
		const int iGlowyGoodnessMax		= 15;
		const double dGlowySpeed		= 0.1;
		
		//MOSTLY system stuff, no touchy beyond this point
		//Unless you know what you are doing
		
		double dGlowyGoodness;
		
		const Microsoft.DirectX.DirectInput.Key ITEM1	 		= Microsoft.DirectX.DirectInput.Key.D1;
		const Microsoft.DirectX.DirectInput.Key ITEM2 			= Microsoft.DirectX.DirectInput.Key.D2;
		const Microsoft.DirectX.DirectInput.Key ITEM3 			= Microsoft.DirectX.DirectInput.Key.D3;
		
		public void Initialize() {
			controller = new SteelBattalionController();
			controller.Init(refreshRate);
			dGlowyGoodness=iGlowyGoodnessMin;
			int baseLineIntensity = 10;//just an average value for LED intensity
			int emergencyLightIntensity = 15;//for stuff like eject,cockpit Hatch,Ignition, and Start
			
			for(int i=4;i<4+30;i++) {
				if (i != (int)ButtonEnum.Eject)//excluding eject since we are going to flash that one
				controller.AddButtonLightMapping((ButtonEnum)(i-1),(ControllerLEDEnum)(i),true,baseLineIntensity);
			}
			
			controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConMain,		true,baseLineIntensity,ITEM1,true);
			controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConSub,		true,baseLineIntensity,ITEM2,true);
			controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConMagazine,	true,baseLineIntensity,ITEM3,true);
			
			joystick = new vJoy();
			acquired = joystick.acquireVJD(1);
			joystick.resetAll();//have to reset before we use it
		}
		
		public int getRefreshRate() {
			return refreshRate;
		}
		
		public void updatePOVhat() {
			controller.POVhat = SBC.POVdirection.CENTER;
		}
		
		//this gets called once every refreshRate milliseconds by main program
		public void mainLoop() {
			updatePOVhat();
			handleComms();
			//Update the glowiness
			dGlowyGoodness = (dGlowyGoodness + dGlowySpeed) % 360;
			//Handle all Axis based stuff222
			joystick.setAxis(1,controller.AimingX,HID_USAGES.HID_USAGE_RX);
			joystick.setAxis(1,controller.AimingY,HID_USAGES.HID_USAGE_RY);
			joystick.setAxis(1,controller.RightPedal-controller.MiddlePedal,HID_USAGES.HID_USAGE_Y);
			joystick.setAxis(1,controller.SightChangeX,HID_USAGES.HID_USAGE_X);
			
			//Handle Special Stuff
			//Determine if the Rotation Lever was used (aka Side Dash of Justice)
			bool bBoost	= false;
			
			//Handle Buttons
			joystick.setButton(controller.GetButtonState(ButtonEnum.RightJoyFire),1,09);		//Right Trigger
			joystick.setButton(controller.GetButtonState(ButtonEnum.RightJoyMainWeapon),1,10);	//Left Trigger
			joystick.setButton(controller.GetButtonState(ButtonEnum.RightJoyLockOn),1,06);		//Right Analog Stick
			joystick.setButton(controller.GetButtonState(ButtonEnum.ToggleOxygenSupply),1,02);	//B Button
			joystick.setButton(controller.GetButtonState(ButtonEnum.ToggleFilterControl),1,11);	//Back Button
			joystick.setButton(controller.GetButtonState(ButtonEnum.LeftJoySightChange),1,01);	//A Button
			
			//Handle the Lights
			controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConMain,getGlowy(0));
			controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConSub,getGlowy(15));
			controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConMagazine,getGlowy(30));
			
			//Handle boost (because it's special)
			joystick.setButton(bBoost,1,08);
			joystick.sendUpdate(1);
		}
		
		public int getGlowy(int iOffset) {
			double angle = ((dGlowyGoodness + iOffset) % 360)*System.Math.PI/180.0;
			return (System.Math.Abs((int)System.Math.Sin(angle))
			  * (iGlowyGoodnessMax-iGlowyGoodnessMin)) + iGlowyGoodnessMin;
		}
		
		bool bComm1=false;
		bool bComm2=false;
		bool bComm5=false;
		public void handleComms() {
			if(controller.GetButtonState(ButtonEnum.Comm1) && !bComm1) {
				bComm1=true;
				Microsoft.DirectX.DirectInput.Key[] Message = {
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.G,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.Return};
				foreach (Microsoft.DirectX.DirectInput.Key msg in Message)
					controller.sendKeyPress(msg);
			} else if(!controller.GetButtonState(ButtonEnum.Comm1)) bComm1=false;
			
			if(controller.GetButtonState(ButtonEnum.Comm2) && !bComm2) {
				bComm2=true;
				Microsoft.DirectX.DirectInput.Key[] Message = {
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.G,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.C,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.L,
					Microsoft.DirectX.DirectInput.Key.L,
					Microsoft.DirectX.DirectInput.Key.E,
					Microsoft.DirectX.DirectInput.Key.C,
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.E,
					Microsoft.DirectX.DirectInput.Key.U,
					Microsoft.DirectX.DirectInput.Key.Return};
				foreach (Microsoft.DirectX.DirectInput.Key msg in Message)
					controller.sendKeyPress(msg);
			} else if(!controller.GetButtonState(ButtonEnum.Comm2)) bComm2=false;
			
			if(controller.GetButtonState(ButtonEnum.Comm5) && !bComm5) {
				bComm5=true;
				Microsoft.DirectX.DirectInput.Key[] Message = {
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.H,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.T,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.W,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.S,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.G,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.O,
					Microsoft.DirectX.DirectInput.Key.D,
					Microsoft.DirectX.DirectInput.Key.Space,
					Microsoft.DirectX.DirectInput.Key.G,
					Microsoft.DirectX.DirectInput.Key.A,
					Microsoft.DirectX.DirectInput.Key.M,
					Microsoft.DirectX.DirectInput.Key.E,
					Microsoft.DirectX.DirectInput.Key.Return};
				foreach (Microsoft.DirectX.DirectInput.Key msg in Message)
					controller.sendKeyPress(msg);
			} else if(!controller.GetButtonState(ButtonEnum.Comm5)) bComm5=false;
		}
		
		public void shutDown() {
			controller.UnInit();
			joystick.Release(1);
		}
	}
}