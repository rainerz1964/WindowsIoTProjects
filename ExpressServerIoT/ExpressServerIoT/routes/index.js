var express = require('express');
var router = express.Router();

var Switch = require('../public/javascripts/Switch.js');

/* GET home page. */
router.get('/', function (req, res) {
    var val = Switch.switches(function (response) {
        res.render('index', { title: 'Lichtanlage', json: response });
    });
});

/* POST react on switch */

router.post('/switching/:id', function (req, res) {
    var id = req.params.id;
    var val = req.body.switch;
    Switch.switchCommand(val, id, function () {
        res.redirect('/');
    });
});

module.exports = router;