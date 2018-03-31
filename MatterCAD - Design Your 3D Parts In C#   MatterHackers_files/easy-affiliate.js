(function (affiliately, $, undefined) {

	var isIE = window.XDomainRequest ? true : false;
	var cross_request = createCrossDomainRequest();
	var url = 'https://www.affiliatly.com/api_request.php';
	var request_mode = '';

	function createCrossDomainRequest(url, handler) {
		var request;
		if (isIE)
			request = new window.XDomainRequest();
		else
			request = new XMLHttpRequest();
		return request;
	}

	function callOtherDomain(post_data) {
		if (cross_request) {
			if (isIE) {
				cross_request.onload = outputResult;
				cross_request.open("POST", url, true);
				cross_request.send(post_data);
			}
			else {
				cross_request.open('POST', url, true);
				cross_request.onreadystatechange = handler; // here we are calling the outputResult()
				cross_request.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
				cross_request.send(post_data);
			}
		}

		return false;
	}

	function handler(evtXHR) {
		if (cross_request.readyState == 4) {
			if (cross_request.status == 200) {
				outputResult();
			}
			else {
				//alert("cross_request Errors Occured");
			}
		}
	}

	function outputResult() {
		var response = cross_request.responseText;
		console.log('Response: ' + response);
		if (( request_mode == 'track' || request_mode == 'update' ) && response.length > 0) {
			var cookie_data = response;
			var expires = cookie_data.match(/&duration=([0-9]+)/);

			if (expires != null) {
				var now = new Date();
				if (request_mode == 'track') {
					var time = now.getTime();
					var expireTime = time + parseInt(expires[1]) * 1000;
					now.setTime(expireTime);
				}
				else {
					expireTime = cookie_data.match(/&expire_time=([0-9]+)/);
					now.setTime(parseInt(expireTime[1]) * 1000);
				}

				document.cookie = "easy_affiliate=" + cookie_data + "; expires=" + now.toGMTString() + "; path=/";
			}
		}
		else if (request_mode == 'mark') {
			//alert( response );
		}
	}

	function getURLParameter(name) {
		return decodeURIComponent((new RegExp('[?|&]' + name + '=' + '([^&;]+?)(&|#|;|$)', 'i').exec(location.search) || [, ""])[1].replace(/\+/g, '%20')) || null
	}

	function getURLHashParameter(name) {
		return decodeURIComponent((new RegExp('[#]' + name + '=' + '([^&;]+?)(&|#|;|$)', 'i').exec(location.hash) || [, ""])[1].replace(/\+/g, '%20')) || null
	}

	function getTrackingParameter() {
		var tracking_parameter = {};
		tracking_parameter.get = {};
		tracking_parameter.hash = {};

		if (getURLParameter('aff') != null)
			tracking_parameter.get.aff = getURLParameter('aff');
		if (getURLParameter('ref') != null)
			tracking_parameter.get.ref = getURLParameter('ref');

		if (getURLHashParameter('aff') != null)
			tracking_parameter.hash.aff = getURLHashParameter('aff');
		if (getURLHashParameter('ref') != null)
			tracking_parameter.hash.ref = getURLHashParameter('ref');

		return tracking_parameter;
	}

	affiliately.startTracking = function(id_affiliatly, aff_override) {
		if (!aff_override) aff_override = null;

		request_mode = 'track';
		var data = affiliately.getCookie('easy_affiliate');
		var tracking_parameter = getTrackingParameter();

		if (aff_override != null){
			console.log('Affiliate override: ' + aff_override);
			tracking_parameter.get.aff = aff_override;
		}

		if ((isEmpty(tracking_parameter.get) == false || isEmpty(tracking_parameter.hash) == false) && data.length == 0) // no cookie at all
		{
			var post_data = 'mode=track-v2&id_affiliatly=' + id_affiliatly + '&tracking_parameter=' + JSON.stringify(tracking_parameter) + '&referer=' + document.referrer;
			console.log('Request: ' + post_data)
			if (getURLParameter('qr') != null)
				post_data += '&qr=1';

			callOtherDomain(post_data);
		}
		else if (data.length > 0) // check the cookie
		{
			var cookie_data = data.split('&');
			cookie_ip = cookie_data[0].split('=');
			cookie_ip = cookie_ip[1];

			if ((isEmpty(tracking_parameter.get) == false || isEmpty(tracking_parameter.hash) == false))
				data += '&store_visit=1';

			if (cookie_ip.length > 0) {
				request_mode = 'update';
				post_data = 'mode=cookie_update&id_affiliatly=' + id_affiliatly + '&' + data;
				callOtherDomain(post_data);
			}
		}
	};

	if (typeof String.prototype.trim !== 'function') {
		String.prototype.trim = function () {
			return this.replace(/^\s+|\s+$/g, '');
		};
	}

	affiliately.getCookie = function (cname) {
		var name = cname + "=";
		var ca = document.cookie.split(';');
		for (var i = 0; i < ca.length; i++) {
			var c = ca[i].trim();
			if (c.indexOf(name) == 0)
				return c.substring(name.length, c.length);
		}

		return "";
	};

	function isEmpty(obj) {
		for (var prop in obj) {
			if (obj.hasOwnProperty(prop))
				return false;
		}

		return true;

	}

}(window.affiliately = window.affiliately || {}, jQuery));
