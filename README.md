XNAMiner
========

Bitcoin Miner for Xbox 360, Windows, Windows Phone, Zune HD, and all XNA supported platforms. 
Based on the Minimal Miner in C# by lithander: 

https://github.com/lithander/Minimal-Bitcoin-Miner

This is basically an XNA port of his C# miner. I've got the code ported to the point where everything builds
without errors. However, since XNA obviously doesn't support Console output, most of the output code will
have to be ported into XNA's Draw() method. Because of that, the structure of the code will have to be changed 
quite a bit in order to accomodate the structure of XNA. All contributors are welcome! 

Note: While this can technically be submitted to Xbox Live Indie Games, it's unlikely it will be approved for
distribution on the marketplace. It's true there are "non-game apps" on the marketplace, but this particular "app"
probably won't pass certification, but it might be worth a try when it's completed. Obviously, anyone can still
sideload this app to their Xbox 360 easily either with a developer/dreamspark account or through some other means. 
This shouldn't be an issue with any other platform though. 
