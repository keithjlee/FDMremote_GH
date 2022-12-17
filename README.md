# FDMremote_GH
FDMremote provides a series of Grasshopper components for Tension/Compression structure form-finding using the Force Density Method.

# Optimization
FDM remote is tied to [FDMremote.jl](https://github.com/keithjlee/FDMremote), which provides a high-performance backend written in Julia for the solving of large networks and/or optimization. It is not required for the core functionality of the plugin.

Communication between the Grasshopper client and the Julia server is through the Websocket protocol. The grasshopper client side is made possible by the [Bengesht](https://github.com/behrooz-tahanzadeh/Bengesht) plugin developed by [Behrooz Tahanzadeh](https://github.com/behrooz-tahanzadeh). 
