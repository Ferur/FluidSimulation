# FluidSimulation
Unity fluid simulation

https://www.youtube.com/watch?v=qGut2IyPMrk

* FluidSimulation.UpdateFluid() is where the fluid is updated. the more this function is called the faster the fluid moves. 
* Fluid is the monobehaviour that controlls the unity side of things. the fluid is split into Fluidparts to match the
TerrainParts of the terrain system. 
* FluidLines are the basic parts of the simulation. they are just horizontal lines of water "pixels". They can be active or inactive.
active FluidLines are currently flowing and changing the terrain/flowing water can set fluidLines to active. The FluidLines are organized 
into a tree and UpdateFluid() takes fluid from the top line and puts in in or under the bottom line. 
* Vertical flow is limited by the size of the line. but horizontal flow isnt limited at all. this is just one limitation of using lines.
maybe a version with fluid quads of different sizes would solve this?

It's been a while since i've wrote this code but leave comments under the youtube video if you have specific questions and i can try
to answer them.
