//If you want a quick overview of how the configuration system works, take a look at SolExodus.cs
//This example was meant to recreate the functionality I displayed for the system in the original release
//however that also means that it is actually pretty complicated.
using SBC;
using System;
using System.Timers;
using System.Runtime.InteropServices;
using System.Text;
//using Microsoft.DirectX.DirectInput;
namespace SBC {
public class DynamicClass
{
bool DEBUGON = false;
System.Windows.Forms.Form aForm;
System.Windows.Forms.TextBox errorBox;

SteelBattalionController controller;
vJoy joystick;
bool acquired;
const int refreshRate = 50;//number of milliseconds between call to mainLoop
const int baseLineIntensity = 5;//just an average value for LED intensity
const int emergencyLightIntensity = 15;//for stuff like eject,cockpit Hatch,Ignition, and Start
int iFadeLight; int iFadeLightSpd = 1;
int iMainLightOn 	= 1;
int iSubLightOn		= 1;
int iMagLightOn		= 1;

const int iPedalThres 			= 64;
const int iPedalBoostThres 		= 192;
const int iRotationLeverThres 	= 64;

const int iOffensiveCoolDown	= 60000;
const int iDefensiveCoolDown	= 60000;
const int iAbilityCoolDown		= 60000;
Timer OffensiveTimer;
Timer DefensiveTimer;
Timer AbilityTimer;

bool bGasDown 		= false;
bool bBreakDown 	= false;
bool bBoostDown 	= false;
bool bisBoosting 	= false;
byte iSightChange	= 0;
byte iStrafing		= 0; //0 - Nothing; 1 - Left; 2 - Right;


//Controls - Movement
const Microsoft.DirectX.DirectInput.Key FORWARD         = Microsoft.DirectX.DirectInput.Key.W;
const Microsoft.DirectX.DirectInput.Key BACKWARD        = Microsoft.DirectX.DirectInput.Key.S;
const Microsoft.DirectX.DirectInput.Key STRAFELEFT      = Microsoft.DirectX.DirectInput.Key.A;
const Microsoft.DirectX.DirectInput.Key STRAFERIGHT     = Microsoft.DirectX.DirectInput.Key.D;
const Microsoft.DirectX.DirectInput.Key JUMP            = Microsoft.DirectX.DirectInput.Key.Space;
const Microsoft.DirectX.DirectInput.Key BOOST           = Microsoft.DirectX.DirectInput.Key.LeftShift;

//Controls - Actions
const Microsoft.DirectX.DirectInput.Key ACTION			= Microsoft.DirectX.DirectInput.Key.E;
const Microsoft.DirectX.DirectInput.Key ABILITY         = Microsoft.DirectX.DirectInput.Key.F;
const Microsoft.DirectX.DirectInput.Key HEAL            = Microsoft.DirectX.DirectInput.Key.C;
const Microsoft.DirectX.DirectInput.Key HOLOTAUNT		= Microsoft.DirectX.DirectInput.Key.H;
const Microsoft.DirectX.DirectInput.Key NORMALTAUNT		= Microsoft.DirectX.DirectInput.Key.G;

//Controls - Navigation & Misc
const Microsoft.DirectX.DirectInput.Key HUDZOOMIN		= Microsoft.DirectX.DirectInput.Key.Equals;
const Microsoft.DirectX.DirectInput.Key HUDZOOMOUT		= Microsoft.DirectX.DirectInput.Key.Minus;
const Microsoft.DirectX.DirectInput.Key SHOWSCORES		= Microsoft.DirectX.DirectInput.Key.Tab;
const Microsoft.DirectX.DirectInput.Key TOGGLEMAP		= Microsoft.DirectX.DirectInput.Key.V;

//Added to support controller (check your settings!)
const Microsoft.DirectX.DirectInput.Key PRIMARYFIRE 	= Microsoft.DirectX.DirectInput.Key.RightControl;
const Microsoft.DirectX.DirectInput.Key SECONDARYFIRE 	= Microsoft.DirectX.DirectInput.Key.Period;
const Microsoft.DirectX.DirectInput.Key ALTFIRE 		= Microsoft.DirectX.DirectInput.Key.Slash;
const Microsoft.DirectX.DirectInput.Key OFFENSIVEITEM 	= Microsoft.DirectX.DirectInput.Key.D1;
const Microsoft.DirectX.DirectInput.Key DEFENSIVEITEM 	= Microsoft.DirectX.DirectInput.Key.D2;

//this gets called once by main program
    public void Initialize() {
	
	if(DEBUGON) {
		aForm = new System.Windows.Forms.Form();

		aForm.Text = @"Hawken SBC Debug";
		errorBox = new System.Windows.Forms.TextBox();
		errorBox.Location = new System.Drawing.Point(11, 11);
		errorBox.Multiline = true;
		errorBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		errorBox.Size = aForm.ClientSize;
		aForm.Controls.Add(errorBox);
		aForm.Show();  // Or just use Show(); if you don't want it to be modal.
	}
	
	controller = new SteelBattalionController();
	controller.Init(50);//50 is refresh rate in milliseconds
	//set all buttons by default to light up only when you press them down
	for(int i=4;i<4+30;i++)
	{
		if (i != (int)ButtonEnum.Eject)//excluding eject since we are going to flash that one
		controller.AddButtonLightMapping((ButtonEnum)(i-1),(ControllerLEDEnum)(i),true,baseLineIntensity);
	}
	controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF3,				true,baseLineIntensity,ACTION,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConMagazine,		true,baseLineIntensity,ABILITY,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.ToggleFilterControl,		true,baseLineIntensity,HEAL,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF1,				true,baseLineIntensity,HOLOTAUNT,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF2,				true,baseLineIntensity,NORMALTAUNT,true);
	
	controller.AddButtonKeyLightMapping(ButtonEnum.MainMonZoomIn,			true,baseLineIntensity,HUDZOOMIN,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.MainMonZoomOut,			true,baseLineIntensity,HUDZOOMOUT,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.ToggleOxygenSupply,		true,baseLineIntensity,SHOWSCORES,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.MultiMonMapZoomInOut,	true,baseLineIntensity,TOGGLEMAP,true);
	
	//controller.AddButtonKeyLightMapping(ButtonEnum.RightJoyMainWeapon,		true,baseLineIntensity,PRIMARYFIRE,true);
	//controller.AddButtonKeyLightMapping(ButtonEnum.RightJoyFire,			true,baseLineIntensity,SECONDARYFIRE,true);
	//controller.AddButtonKeyLightMapping(ButtonEnum.RightJoyLockOn,			true,baseLineIntensity,ALTFIRE,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConMain,			true,baseLineIntensity,OFFENSIVEITEM,true);
	controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConSub,			true,baseLineIntensity,DEFENSIVEITEM,true);
	
	joystick = new vJoy();
	acquired = joystick.acquireVJD(1);
	joystick.resetAll();//have to reset before we use it
	
	OffensiveTimer = new Timer();
    OffensiveTimer.Elapsed+=new ElapsedEventHandler(OffensiveTimerElasped);
	OffensiveTimer.Interval = iOffensiveCoolDown;
	OffensiveTimer.AutoReset = false;
	
	DefensiveTimer = new Timer();
    DefensiveTimer.Elapsed+=new ElapsedEventHandler(DefensiveTimerElasped);
	DefensiveTimer.Interval = iDefensiveCoolDown;
	DefensiveTimer.AutoReset = false;
	
	AbilityTimer = new Timer();
    AbilityTimer.Elapsed+=new ElapsedEventHandler(AbilityTimerElasped);
	AbilityTimer.Interval = iAbilityCoolDown;
	AbilityTimer.AutoReset = false;
}

//Timers
private void OffensiveTimerElasped(object source, ElapsedEventArgs e) {
	 iMainLightOn = 1;
}
private void DefensiveTimerElasped(object source, ElapsedEventArgs e) {
	 iMainLightOn = 1;
}
private void AbilityTimerElasped(object source, ElapsedEventArgs e) {
	 iMagLightOn = 1;
}

//this is necessary, as main program calls this to know how often to call mainLoop
public int getRefreshRate()
{
  return refreshRate;
}

private uint getDegrees(double x,double y)
	{
		uint temp = (uint)(System.Math.Atan(y/x)* (180/Math.PI));
		if(x < 0)
			temp +=180;
		if(x > 0 && y < 0)
			temp += 360;
			
		temp += 90;//origin is vertical on POV not horizontal
			
		if(temp > 360)//by adding 90 we may have gone over 360
			temp -=360;
		
		temp*=100;
		
		if (temp > 35999)
			temp = 35999;
		if (temp < 0)
			temp = 0;
			
		return temp;
	}

//this gets called once every refreshRate milliseconds by main program
public void mainLoop()
{
	if(DEBUGON) {
		aForm.Update();
		errorBox.Text = controller.AimingX.ToString() + "\n\n";
	}
	joystick.setButton(controller.GetButtonState(ButtonEnum.ToggleVTLocation),1,31);
	joystick.setAxis(1,controller.AimingX,HID_USAGES.HID_USAGE_X);
	joystick.setAxis(1,controller.AimingY,HID_USAGES.HID_USAGE_Y);
	joystick.setAxis(1,(controller.RightPedal - controller.MiddlePedal),HID_USAGES.HID_USAGE_Z);//throttle
	joystick.setAxis(1,controller.RotationLever,HID_USAGES.HID_USAGE_RZ);
	joystick.setAxis(1,controller.SightChangeX,HID_USAGES.HID_USAGE_SL0);
	joystick.setAxis(1,controller.SightChangeY,HID_USAGES.HID_USAGE_RX);  
	joystick.setAxis(1,controller.LeftPedal,HID_USAGES.HID_USAGE_RY);    
	joystick.setAxis(1,controller.GearLever,HID_USAGES.HID_USAGE_SL1);
	
	bool bTemp = false;
	//Gas Pedal Check
	if(controller.RightPedal > iPedalThres) bTemp = true;
	if(bGasDown!=bTemp) {
	if(bTemp)
		controller.sendKeyDown(FORWARD);
	else
		controller.sendKeyUp(FORWARD);
	}
	bGasDown=bTemp; bTemp = false;
	//Boost Check
	if(controller.RightPedal > iPedalBoostThres) bTemp = true;
	if(bisBoosting!=bTemp) {
		if(bTemp) {
			controller.sendKeyUp(FORWARD);
			controller.sendKeyDown(BOOST);
			System.Threading.Thread.Sleep(35);
			controller.sendKeyDown(FORWARD);
		} else {
			controller.sendKeyUp(BOOST);
			controller.sendKeyUp(FORWARD);
			System.Threading.Thread.Sleep(25);
			controller.sendKeyDown(FORWARD);
		}
	}
	bisBoosting=bTemp; bTemp=false;
	//Break(Reverse) Pedal Check
	if(controller.MiddlePedal > iPedalThres) bTemp = true;
	if(bBreakDown!=bTemp) {
		if(bTemp)
			controller.sendKeyDown(BACKWARD);
		else
			controller.sendKeyUp(BACKWARD);
	}
	bBreakDown=bTemp; bTemp = false;
	//Boost(Jump) Pedal Check
	if(controller.LeftPedal > iPedalThres) bTemp = true;
	if(bBoostDown!=bTemp) {
		if(bTemp)
			controller.sendKeyDown(JUMP);
		else
			controller.sendKeyUp(JUMP);
	}
	bBoostDown=bTemp; bTemp = false;
	//Rotation(Strafe) Stick Check
	int iTemp = 0;
	if(controller.RotationLever > iRotationLeverThres) iTemp = 2;
	if(controller.RotationLever < -iRotationLeverThres) iTemp = 1;
	if(iStrafing!=iTemp) {
		controller.sendKeyUp(STRAFELEFT);
		controller.sendKeyUp(STRAFERIGHT);
		if(iTemp==1)
			controller.sendKeyDown(STRAFELEFT);
		else if(iTemp==2)
			controller.sendKeyDown(STRAFERIGHT);
	}
	iStrafing=(byte)iTemp; iTemp = 0;
	
	int SCX = controller.SightChangeX;
	int SCY = controller.SightChangeY;
	if((Math.Abs(SCX) > 25) || (Math.Abs(SCY) > 25)) {
		iTemp = (int)getDegrees(controller.SightChangeX,controller.SightChangeY);
		if(iTemp > 15000 && iTemp < 21000) iTemp = 2;
		else if(iTemp > 6000 && iTemp < 12000) iTemp = 3;
		else if(iTemp > 24000 && iTemp < 30000) iTemp = 4;
		else if((iTemp > 33000 && iTemp < 35999) || (iTemp > 0 && iTemp < 3000)) iTemp = 1;
		else iTemp = 0;
	} else {
		iTemp = 0;
	}
	if(iSightChange != iTemp) {
		if(iTemp == 1) {
			controller.sendKeyDown(BOOST);
		} else {
			controller.sendKeyUp(BOOST);
		}
		if(iTemp == 2) {
			controller.sendKeyDown(BOOST);
			System.Threading.Thread.Sleep(25);
			controller.sendKeyDown(BACKWARD);
			System.Threading.Thread.Sleep(25);
			controller.sendKeyUp(BOOST);
			controller.sendKeyUp(BACKWARD);
		} else if(iTemp == 4) {
			controller.sendKeyDown(OFFENSIVEITEM);
			controller.sendKeyUp(OFFENSIVEITEM);
			OffensiveTimer.Start();
			iMainLightOn = 0;
		} else if(iTemp == 3) {
			controller.sendKeyDown(DEFENSIVEITEM);
			controller.sendKeyUp(DEFENSIVEITEM);
			DefensiveTimer.Start();
			iSubLightOn = 0;
		}
	} iSightChange = (byte)iTemp; iTemp = 0;

	joystick.setContPov(1,getDegrees(controller.SightChangeX,controller.SightChangeY),1);
	for(int i=1;i<=29;i++)
	{
		joystick.setButton((bool)controller.GetButtonState(i-1),(uint)1,(char)(i-1));
	}
	
	//Glowiness
	iFadeLight += iFadeLightSpd;
	if(iFadeLight==15) iFadeLightSpd = -iFadeLightSpd;
	if(iFadeLight==0) iFadeLightSpd = -iFadeLightSpd;
	//Light mangagement for the weapons
	if(controller.GetButtonState(ButtonEnum.WeaponConMain)) {
		OffensiveTimer.Start();
		iMainLightOn = 0;
	}
	if(controller.GetButtonState(ButtonEnum.WeaponConSub)) {
		DefensiveTimer.Start();
		iSubLightOn = 0;
	}
	if(controller.GetButtonState(ButtonEnum.WeaponConMagazine)) {
		AbilityTimer.Start();
		iMagLightOn = 0;
	}
	if(controller.GetButtonState(ButtonEnum.Start)) {
		AbilityTimer.Enabled = DefensiveTimer.Enabled = OffensiveTimer.Enabled = false;
		iMagLightOn = iSubLightOn = iMainLightOn = 1;
	}
	controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConMain,iFadeLight*iMainLightOn);
	controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConSub,iFadeLight*iSubLightOn);
	controller.SetLEDState((ControllerLEDEnum)ButtonLights.WeaponConMagazine,iFadeLight*iMagLightOn);

  joystick.sendUpdate(1);
}

//this gets called at the end of the program and must be present, as it cleans up resources
public void shutDown()
{
  controller.UnInit();
  joystick.Release(1);
}

}
}