# Rimbody

## Features
* Gain/lose muscle and fat based on pawn's diet and activities
* Dynamic Bodytype based on muscle and fat
* Workout schedule
* Strength, Balance, Cardio workouts

## Description
Rimbody allows your pawns to gain or lose muscle and fat based on their diet and activities, and change their body type accordingly. This mod includes a patch for GetRimped that fixes its bugs and enables your pawns to use workout machines to actually get ripped. I highly recommend using it, as Rimbody currently does not offer any bodyweight strength training.

The aim of this mod is not to accurately emulate real life, but to add consequences to the type of work pawns do by creating tangible differences between pawns who mostly sit around all day and those who engage in hard manual labor.
Pawns who do not use their bodies as much will need to spend time working out to compensate for their lack of activity in order to stay fit.

## Rimbody
Rimbody assigns fat and muscle to every pawn. The fat and muscle of your colonists, prisoners, and slaves change based on their diet and activities, while enemies and guests will have fixed amounts of fat and muscle.<br/>
You can view information about a pawn's fat and muscle in the Rimbody window. You can find Rimbody Button in your pawn's Bio tab

[Rimbody Button]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/RimbodyButton.png?raw=true" alt="RimbodyButton" width="400"/> 

[Rimbody Window]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/RimbodyWindow.png?raw=true" alt="RimbodyWindow" width="400"/> 

### Body Fat
Body fat represents how fat a pawn is relative to their size.<br/>
Pawns gain Body fat when their food need is above 25 and lose fat when their food need drops below 25. Additionally, pawns lose fat based on their current activities: mining pawns burn a significant amount of fat, moving pawns burn some fat, while pawns doing sedentary jobs burn very little fat.

### Muscle Mass
Muscle mass represents how muscular a pawn is relative to their size.<br/>
Pawns lose muscle passively, particularly if they are already bulky or when they are hungry. Pawns gain muscle based on their current activities, but this gain is not applied directly to them; instead, it is stored in a "reserve."

### Reserve
When your pawns rest, the gains stored in the reserve are slowly converted into muscle.<br/>
The maximum amount of reserve depends on how much muscle your pawn already has, so scrawny pawns will fill their reserve faster than hulking pawns.<br/>
A tired or drowsy pawn will lose the gains stored in the reserve, so be sure they get enough rest! (6 to 7 hours of sleep is sufficient, even for hulking bodybuilder pawns.)<br/>
Pawns that do not need sleep will not utilize the reserve mechanics; instead, they will gain muscle immediately.

### Workout
You can set a pawn's schedule to "work out" to make them exercise. There are three types of workouts:

* Strength: Pawns focus on gaining muscle. Currently, there are no bodyweight exercises available (coming soon!), so use Get Rimped if you want efficient strength training.
* Balance: Pawns gain some muscle and lose some fat simultaneously. Get Rimped's balance beam and yoga ball are considered balance workouts.
* Cardio: Pawns focus on losing fat. The treadmill and exercise bike are classified as cardio workouts. Your pawns will also go jogging; jogger pawns will prefer jogging over using machines and receive a mood bonus.

Even when doing the same type of workout, pawns will try to diversify their routines.<br/>
Using the same machine multiple times will reduce workout efficiency, so it's a good idea to provide a variety of workout machines.<br/>

[Pawns getting Full benefit]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/FullBenefit.png?raw=true" alt="RimbodyButton" width="200"/> 

[Pawns getting reduced benefit]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/ReducedBenefit.png?raw=true" alt="RimbodyWindow" width="200"/> 

### Goals
You can set goals for a pawn, and they will strive to achieve them.

* When a pawn's diet goal is set below their current fat level, the pawn will refrain from eating unless they are hungry and will prioritize cardio exercises over strength exercises during their workout time.

* When a pawn's gain goal is set above their current muscle level, the pawn will try to eat more frequently to avoid losing muscle and will focus on strength exercises rather than cardio exercises during their workout time.

### BodyType Change
A pawn's body type is determined by their fat and muscle levels. The body shape corresponding to their fat and muscle is shown below.

<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/RimbodyChart.png?raw=true" alt="RimbodyChart" width="500"/>

    
There is a grace period around the thresholds. Your pawns need to have 0.5 (configurable in settings) more or less fat/muscle than the threshold for their body type to change.<br/>
For example, if the fat threshold for Fat bodytype is set to 35, a standard pawn needs to reach 36.5 fat for their bodytype to change, while a fat pawn needs to drop to 34.5 fat for their bodytype to change to Standard.

### Refresh button
If due to mod conflicts or some reason your pawn ended up with a bodytype that doesn't match their current bodyfat and muscle level, you can click the refresh button to set their bodytype back.
Refresh button only appears in Edit mode.

### Other factors
Young pawns (aged 13–25) will gain muscle and lose fat more easily. As pawns age, it becomes more difficult for them to stay fit.

Male pawns find it easier to gain muscle, while female pawns require a bit more fat to appear fat (this is configurable in the settings).

## FAQ

<br/>Q: Is this mod compatibile with [Insert Mode Name Here]?
* Probably? However, mods that modify how a pawn gains rest or food needs may cause issues.
* HAR races do not get Rimbody values unless they share the exact same lifestages and bodytype as vanilla pawns. Biotech Xenotypes should be compatible.
* Please keep in mind that I haven't been able to test all mods, so report any incompatibilities you encounter.

<br/>Q: How performance heavy is this mod?
* This mod piggybacks on food needs and doesn’t have its own ticking methods for tracking pawns' fat and muscle, so one less ticking for you.
* There are some inevitable condition checks added to food needs as a result, but these mostly involve checking for null values. I’ve aimed to keep the calculations as light as possible, primarily relying on elementary arithmetic. You can see the detailed calculations in the Detailed Mechanism section below.
* Below is the comparison of food need calculation with this mod off and on. Of course, this can differ with your mod lists, but it should have a minimal impact, hopefully.
 
[No Rimbody]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/norimbody.png?raw=true" alt="RimbodyChart" width="800"/>

[With Rimbody]<br/>
<img src="https://github.com/jagerguy36/Rimbody/blob/main/Images/rimbody.png?raw=true" alt="RimbodyChart" width="800"/> 

<br/>Stuffs like jumpropes disappeared from Get Rimped after installing this mod! Where did they go?
* Some of the Get Rimped items, such as barbells and jumpropes, are better suited as items rather than buildings. I plan to make them available soon; you can find more details in the Planned Features section. For now, I’ve disabled them to avoid confusion.

<br/>Q: Ideology Content?
* Unlikely. Since I do not own the Ideology expansion, I can't dev/test it.

<br/>Q: Is this mod save game compatible?
* If your saved game includes any buildings from GetRimped, you should deconstruct them before adding this mod. You may encounter a one-time error when first loading if you had GetRimped before, but otherwise, you should be good to go.

<br/>Q: Can I remove it from an existing save?
* If you have any buildings from the GetRimped mod, you should deconstruct them before removing this mod. You may encounter a one-time error on the first load.
* You will still be left with any body type changes made by this mod (for example, pawns with the Hulking body gene whose body type changed to Standard will still have the Standard body type).
* Other than that, this mod is mostly self-contained and should leave little to no effect. However, removing a mod mid-save is generally not recommended, and I cannot guarantee that no issues will arise.

## Mod Integration

* Rimbody offers additional functionality when paired with the mods listed here.

Mod | Content
--- | ---
Biotech |  If your pawn has a specific gene that enforces a certain body type, the gain/loss rate of fat and muscle will be adjusted accordingly. This means a pawn with a thin-bodytype gene CAN have standard bodyType, but they will most likeyl revert back to thin very soon. Non-senescent pawns' bodies will be considered to be at age 25 (configurable in settings) if they get past this age. Pawns who do not need sleep will gain muscle without requiring rest.
GetRimped | Pawns will use workout equipment during their workout schedule. Also fixes its bugs and Misc. Training compatibility problems when loaded together
SYR Individuality [Continued] | The Rimbody window is merged with the Individuality window. Body weight now depends on muscle and fat.
Big and Small - Framework | Androgynous body is correctly tracked. Mimic will take on the fat and muscle of the target.
Toddlers | Baby wiggling and toddler play are considered strength and balance activities.
Human Power Generator Mod | Cycling is classified as a cardio activity.
Quary | Quarrying is considered hard manual labor.
Rimfeller | Deep drilling is classified as hard manual labor.
Misc. Training | Martial Art practice is classified as manual labor.

If there are any other mod-added jobs that you think should be recognized as manual labor, please suggest them in the Suggestions Discussion.

## Incompatibilities

### Hard Incompatibility
Currently, I am not aware of any hard incompatibilities. If you find one, please report it and let me know.

### Soft Incompatibility
* You drive, I rest: Pawns resting in moving vehicles won't convert their gains to muscle gain. You can think of this as having a fitful sleep that interrupts recovery.

## Known Issues

* Currently when you load this with GetRimped, you get translation warning. This is because I removed some of the defs from Get Rimped, and it will be fixed soon when I add those defs back as items.

## Planned Features
* Expand GetRimped Compatibility<br/>
Make jumpropes and barbells into items that pawns can use for exercise. A rack will serve as a storage building for these items and will provide a buff to the gym it’s installed in.

* Add bodyweight Excercise:<br/>
Add push-ups so your pawns can engage in strength training even without gym equipment.

* Add primitive workout methods:<br/>
Allow pawns to do strength training using stone chunks.

* Add animation:<br/>
Add animations to workout


## Modules
* Rimbody Stats Module (planned)<br/>
Make it so that Muscle, Fat amount affect pawn stats like running speed, immunity gain, carry abbility.

* Rimbody Health Module (planned)<br/>
Add health conditions for pawns based on their muscle and fat levels. Sleep Apnea for high fat, "Pumped" buff after doing strength workout etc.

* Rimbody Supplement Module (planned)<br/>
Protein shake, Diet pills, Steroids, etc. for your pawns


## Detailed Mechanism

BodyFat and MuscleMass are updated when your pawn's FoodNeed is updated. This means that if your pawn has no Food Need, fat and muscle levels will not be updated. This is by design, as body changes require food intake and a functioning metabolism.

Gain and Loss Factor are adjusted by agefactor, which is (age-25)/1000 for fat and (age-25)/200 for muscle.

### Fat
Fat change is calculated as below

    FatGain = (FoodNeed^(1/2))
    FatLoss = (CurrentFat + 60) / 50
    FatChange = ( FatGain * GainFactor ) - ( FatLoss * Lossfactor * CardioFactor )

* At 100% FoodNeed ( 1 ), Fat gain becomes 1
* CardioFactor takes into account the job your pawn is doing when fat is updated. Pawns doing nothing has CardioFactor of 0.35, pawns moving while working have 1.2, pawns sprinting have 2.0 and so on.

### Muscle
Muscle change is calculated as below

    MuscleGain = 0.4  * ( 5 + (CurrentMuscle+75) / (CurrentMuscle-75) )
    ReserveMax = 2*CurrentMuscle+100
    Stored = MuscleGain * GainFactor * StrengthFactor
    MuscleLoss = ( (CurrentMuscle+60) / 125) * ( (FoodNeed+12.5) / 12.5 )^(-1/2)
    MuscleChange (awake) = - MuscleLoss * LossFactor
    MuscleChange (resting) = SwolFactor * RestEffectiveness (subtracted from Reserve) - MuscleLoss * LossFactor

* StrengthFactor takes into account the job your pawn is doing. Strength workouts have a factor of 2.0, while hard labor activities like mining and construction have a factor of 1.3. Moving gives a factor of 0.4, and other stationary work provides a factor of 0.2.


## References

<a href="https://www.flaticon.com/free-icons/strong" title="strong icons">Strong icons created by DinosoftLabs - Flaticon</a><br/>
<a href="https://www.flaticon.com/free-icons/cardio" title="cardio icons">Cardio icons created by meaicon - Flaticon</a><br/>
<a href="https://www.flaticon.com/free-icons/full-body" title="full body icons">Full body icons created by Any Icon - Flaticon</a><br/>
<a href="https://www.flaticon.com/free-icons/refresh" title="refresh icons">Refresh icons created by Freepik - Flaticon</a>
