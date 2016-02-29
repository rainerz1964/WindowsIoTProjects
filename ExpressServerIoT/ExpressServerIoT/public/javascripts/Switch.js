request = require ('request')
var server = 'RainersIoT';
var port = 8888;

function buildUrl(meth) {
     return 'http://' + server + ':' + port.toString() + meth;
}

module.exports = 
{
    switchCommand : function (val, id, callback) {
        var base = '/api/switch/' + id.toString();
        var meth = base + (val ? '/on' : '/off');
        request({
            url: (buildUrl(meth)),
            json: true,
            method : 'PUT'
        }, function (error, response, body) {
            if (!error && response.statusCode === 200) {
                return callback();
            }
        }) 
    },
    switches : function (callback) {
        var meth = '/api/switches';
        request({
            url: (buildUrl(meth)),
            json: true,
            method : 'GET'
        }, function (error, response, body) {
            if (!error && response.statusCode === 200) {
                return callback (body);
            }
        }) 
    }
}