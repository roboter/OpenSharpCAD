var loader_element = '<img src="/static/ajax-spin-blue.gif" style="height:16px;width:16px;" alt="...">';
var display_count = 0;

$(document).ready(function () {
	// init
	displayInStockProducts();
	loadSessionData();
	loadCartData();
	bindAddToDigitalLibrary();
	bindDownloadDigitalItem();
	cartWidgetBindAddToCart();
	bindScrollToAnchor();
	bindOutOfStockToast();
	bindTopScrollSlide();
	bindProductSearch();
	bindNewsletterSignup();
	bindEmbedYoutube();
	bindAddOns();

	// setup
	var windowWidth = window.innerWidth;

	$('#js-nav').prepend('<div class="js-overlay overlay-css" data-view="closed" style="display:none;"></div>'); // add overlay into html
	$('#js-nav .nav-main').append('<span class="close" id="js-nav-close"></span>'); // add close nav button for mobile

	if (windowWidth <= 1000) {
		bindNavDropdownMenuResponsive();
		bindNavMainResponsive();
	} else {
		bindNavDropdownMenu();
	}

	//collections
	productCollectionLoadWidgets();
	// products
	var numProductLoadedFalse = productSingleLoadWidgets(50); // load first products, get how many are left
	if (numProductLoadedFalse > 0) { // if more products still aren't loaded
		bindProductSingleWidgetsScroll(); // bind scroll load
	}

	// cosmetic
	loadArticleWidgetSingle();
	bindAvailableProductNoticeSignup();
	loadArticleWidgets();

	// search affix -- check for mobile (for IE)
	var isMobile = window.matchMedia("only screen and (max-width: 770px)");
	if (isMobile.matches) {
		bindSearchAffix();
	}

	// after resize
	$(window).resize(debounce(function () { // when resizing windows, bind as necessary
		bindNavigation();
		bindEmbedYoutube();
	}, 350));

	highlightSideNavigation();
	// bindStatus();
});

// function bindStatus(){
// 	var current = new Date();
// 	var start = new Date('2017-11-17T00:00:00.000-08:00');
// 	var end = new Date('2017-11-18T00:00:00.000-08:00');

// 	if (current.getTime() > start.getTime()) {
// 		if (current.getTime() < end.getTime()) {
// 			var status = "active";
// 		} else {
// 			var status = "post";
// 		}
// 	} else {
// 		var status = "pre";
// 	}

// 	var previewStatus = getUrlParameter('status');
// 	if (previewStatus && previewStatus.length > 0) {
// 		var status = previewStatus;
// 	}

// 	if (status == "pre") {
// 		$('body').addClass('status-pre');
// 	} else if(status == "active") {
// 		$('#status').show();
// 		$('body').addClass('status-active');
// 	} else {
// 		$('body').addClass('status-post');
// 	}
// }

var getUrlParameter = function getUrlParameter(sParam) {
	var sPageURL = decodeURIComponent(window.location.search.substring(1)),
		sURLVariables = sPageURL.split('&'),
		sParameterName,
		i;

	for (i = 0; i < sURLVariables.length; i++) {
		sParameterName = sURLVariables[i].split('=');

		if (sParameterName[0] === sParam) {
			return sParameterName[1] === undefined ? true : sParameterName[1];
		} else {
			return ''; // return blank if tag not found
		}
	}
};

// limit the firing of a function
function debounce(func, wait, immediate) {
	var timeout;
	return function () {
		var context = this, args = arguments;
		var later = function () {
			timeout = null;
			if (!immediate) func.apply(context, args);
		};
		var callNow = immediate && !timeout;
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
		if (callNow) func.apply(context, args);
	};
}

function bindNavigation() {
	var windowWidth = window.innerWidth;

	if (windowWidth <= 1000) {
		bindNavMainResponsive();
		bindNavDropdownMenuResponsive();
		bindAccountMenuResponsive();
	} else {
		bindNavDropdownMenu();
		bindAccountMenu();
	}

	bindLogoutLink();
	bindSigninLink();
}

function bindLogoutLink() {
	$('.js-account-logout')
		.unbind('click')
		.click(function () {
			window.location.href = '/accounts/logout?continue=' + window.location.href;
		});
}

function bindSigninLink() {
	$('.js-account-signin')
		.unbind('click')
		.click(function () {
			window.location.href = '/accounts/login?continue=' + window.location.href;
		});
}

function bindTopScrollSlide() {
	var windowWidth = window.innerWidth;

	if (windowWidth >= 1000) {
		var isvisible = false;

		$(window).scroll(debounce(function () {
			if ($('.nav').inView('bottomOnly') == false && isvisible == false) {
				$('.top-site').animate({width: 'toggle'}, 250);
				isvisible = true;
			}
			if ($('.nav').inView('bottomOnly') == true && isvisible == true) {
				$('.top-site').animate({width: 'toggle'}, 250);
				isvisible = false;
			}
		}, 300));

		$.fn.inView = function (inViewType) {
			var viewport;
			viewport = {};
			viewport.top = $(window).scrollTop();
			viewport.bottom = viewport.top + $(window).height();
			var bounds = {};
			bounds.top = this.offset().top;
			bounds.bottom = bounds.top + this.outerHeight();
			switch (inViewType) {
				case 'bottomOnly':
					return ((bounds.bottom <= viewport.bottom) && (bounds.bottom >= viewport.top));
				case 'topOnly':
					return ((bounds.top <= viewport.bottom) && (bounds.top >= viewport.top));
				case 'both':
					return ((bounds.top >= viewport.top) && (bounds.bottom <= viewport.bottom));
				default:
					return ((bounds.top >= viewport.top) && (bounds.bottom <= viewport.bottom));
			}
		};
	}
}

function bindAccountMenu() {
	$('.top-account').unbind('click').unbind('mouseenter mouseleave');
	var accountStatus = $('#js-account').attr('data-account-status');

	if (accountStatus == 'signed-out') {
		$('.js-account').addClass('js-account-signin');
	} else {
		$(".top-account").hover(function () {
			$('#top-account-menu').slideDown(150);
			$(this).attr('data-view', 'open');
		}, function () {
			$('#top-account-menu').stop().slideUp(280);
			$(this).attr('data-view', 'closed');
		});

	}
}

function bindAccountMenuResponsive() {
	$('.nav-account').unbind('click').unbind('mouseenter mouseleave');
	var accountStatus = $('#js-account').attr('data-account-status');

	if (accountStatus == 'signed-out') {
		$('.js-account').addClass('js-account-signin');
	} else {
		$('.nav-account').click(function () {
			if ($(this).attr('data-view') == 'closed') {
				$("#nav-account-menu").slideDown('fast');
				$(this).attr('data-view', 'open');
			} else {
				$("#nav-account-menu").slideUp('fast');
				$(this).attr('data-view', 'closed');
			}

		});
	}
}

function bindNavDropdownMenu() {
	$('#js-nav .nav-dropdown-button > a').unbind();	// undo bindNavDropdownResponsive

	var timer;

	$('#js-nav .nav-dropdown-button')
		.on("mouseover", function () {
			clearTimeout(timer);
			$(this).find('a').first().addClass('nav-active');

			var current = $(this).find('.nav-dropdown-item');
			current.show().attr('data-view', 'open');
			$('.nav-dropdown-item').not(current).hide().attr('data-view', 'closed');

		}).on("mouseleave", function () {
		var $this = $(this);
		$this.closest('.nav-dropdown-button').find('.nav-active').removeClass('nav-active');
		timer = setTimeout(
			function () {
				$this.find('.nav-dropdown-item').hide().attr('data-view', 'closed');
			}
			, 600);
	});
}

function bindNavDropdownMenuResponsive() {
	$('#js-nav .nav-dropdown-button').unbind(); // undo bindNavDropdownMenu

	$('#js-nav .nav-dropdown-button > a')
		.unbind("mouseenter mouseleave touch click")
		.on('touch click', function () {
			event.preventDefault();
			var dropdown = $(this).parent().find('.nav-dropdown-item');
			if ($(dropdown).attr('data-view') == 'closed') {
				$(dropdown).slideDown('fast');
				$(dropdown).attr('data-view', 'open');
			} else {
				$(dropdown).slideUp('fast');
				$(dropdown).attr('data-view', 'closed');
			}
		});
}

function bindNavMainResponsive() {
	var trigger = $('#js-nav .navbar-toggle, #js-nav .js-overlay, #js-nav-close'),
		overlay = $('#js-nav .js-overlay'),
		isClosed = false;

	trigger.unbind();
	trigger.on('click touch', function () {
		pushBody();
	});

	function pushBody() {
		if (isClosed == true) {
			overlay.toggle();
			isClosed = false;
			trigger.attr('data-view', 'closed');
			$('body').toggleClass('toggled');
		} else {
			overlay.toggle();
			isClosed = true;
			trigger.attr('data-view', 'open');
			$('body').toggleClass('toggled');
		}
	}
}

function loadCartData() {
	$.ajax({
		type: "POST",
		url: '/data/cart-info',
		data: {},
		dataType: 'json',
		beforeSend: function (x) {
			if (x && x.overrideMimeType) {
				x.overrideMimeType("application/json;charset=UTF-8");
			}
		},
		success: function (data) {
			var status = data['Status'];
			if (status == 'success') {
				var cart_items = data['CartItems'];
				$('.js-cart-count').html(cart_items);
			}
		},
		error: function (xhr) {
			return false;
		}
	});
}

function loadSessionData() {
	$.ajax({
		type: "POST",
		url: '/data/session-info',
		data: {},
		dataType: 'json',
		beforeSend: function (x) {
			if (x && x.overrideMimeType) {
				x.overrideMimeType("application/json;charset=UTF-8");
			}
		},
		success: function (data) {
			var status = data['Status'];
			if (status == 'success') {
				var has_session = data['AuthSession'];
				if (has_session == true) {
					var user_name = data['UserName'];
					$('.js-account-username').html(user_name);
					$('.js-account')
						.attr('data-account-status', 'signed-in')
						.unbind('click');
				} else {
					$('.js-account-username')
						.html('Sign In');
					$('.js-account')
						.attr('data-account-status', 'signed-out')
						.addClass('js-account-signin');
				}
			}

			var windowWidth = window.innerWidth;

			if (windowWidth <= 1000) {
				bindAccountMenuResponsive();
			} else {
				bindAccountMenu();
			}

			bindLogoutLink();
			bindSigninLink();
		},
		error: function (xhr) {
			return false;
		}
	});
}

function bindEmbedYoutube() {
	if (typeof embedVideoActivated === 'undefined') {
		$(".embed-youtube").each(function () {
			var youtubeId = $(this).attr('data-embed');

			var iframe = document.createElement("iframe"); // setup iframe
			iframe.setAttribute("frameborder", "0");
			iframe.setAttribute("allowfullscreen", "");

			var isMobile = window.matchMedia("only screen and (max-width: 760px)"); // determine if mobile

			if (!isMobile.matches) { // desktop, run below
				var youtubeLoaded = $(this).attr('data-loaded');

				if (!youtubeLoaded) { // if not loaded
					var youtubeEmbedSize = $(this).attr('data-size'), // youtube preview size to load
						youtubeImageCustom = $(this).attr('data-image'), // custom overlay image
						youtubeEmbedAttributes = $(this).attr('data-attributes'), // youtube attributes
						youtubeImageClass = '',
						youtubeImageReference = '';

					if (!youtubeEmbedAttributes) {
						youtubeEmbedAttributes = "rel=0&showinfo=0&autoplay=1"; // default attributes
					}

					if (!youtubeImageCustom) {
						if (youtubeEmbedSize) { // size of youtube preview
							switch (youtubeEmbedSize) {
								case 'xs':
									youtubeImageSize = "sddefault";
									break;
								case 'sm':
									youtubeImageSize = "sddefault";
									break;
								case 'md':
									youtubeImageSize = "sddefault";
									break;
								case 'lg':
									youtubeImageSize = "hqdefault";
									break;
								default:
									youtubeImageSize = "sddefault"; // default attributes
									break;
							}
						} else {
							youtubeImageSize = "sddefault"; // default attributes
						}

						youtubeImageReference = "https://img.youtube.com/vi/" + youtubeId + "/" + youtubeImageSize + ".jpg";
						youtubeImageClass = 'youtube-image-' + youtubeImageSize;
					} else {
						youtubeImageReference = youtubeImageCustom;
						youtubeImageClass = 'youtube-image-custom';
					}

					iframe.setAttribute("src", "https://www.youtube.com/embed/" + youtubeId + "?" + youtubeEmbedAttributes);

					// preview image
					var youtubeImage = "<img src='" + youtubeImageReference + "' alt='YouTube Video' class=" + youtubeImageClass + " />";

					// compiled
					var playButton = '<div class="play-button"></div>';
					$(this).append(playButton + youtubeImage);

					// action
					$(this).unbind().click(function () {
						$(this).html(iframe);
						embedVideoActivated = 1;
					});

					// set attribute to loaded
					$(this).attr('data-loaded', "true");
				}
			} else {
				// no autoplay if mobile
				iframe.setAttribute("src", "https://www.youtube.com/embed/" + youtubeId + "?rel=0&showinfo=0");
				$(this).html(iframe);
				embedVideoActivated = 1;
			}
		});
	}
}

function loadArticleWidgetSingle() {
	$('.article-single[data-article-loaded="false"]').each(function () {
		var articleKey = $(this).attr('data-article-key');
		var articleLayout = $(this).attr('data-article-layout');
		var articleCategory = ''; // $(this).attr('data-article-category')
		getArticleWidget(articleKey, articleLayout, articleCategory);
	});
}

function getArticleWidget(akey, layout, category) {
	$.ajax({
		type: "POST",
		url: '/article-widget/' + akey,
		data: {
			layout: layout,
			category: category
		},
		dataType: 'json',
		beforeSend: function (x) {
			if (x && x.overrideMimeType) {
				x.overrideMimeType("application/json;charset=UTF-8");
			}
		},
		success: function (data) {
			var content = data['content'];
			$('[data-article-loaded="false"][data-article-key=' + akey + ']').html(content).attr('data-article-loaded', 'true');
		},
		error: function (xhr) {
			$('[akey=' + akey + ']').html('unable to load');
			return;
		}
	});
}

function loadArticleWidgets() {
	$('.article-collections').each(function () {
		var articleTagName = $(this).attr('data-collection-tag');
		getArticleWidgets(articleTagName);
	});
}

function getArticleWidgets(articleTagName) {
	var data = $('[data-collection-tag="' + articleTagName + '"]').data();
	$.ajax({
		type: "POST",
		url: '/get-article-widgets-from-tag/' + articleTagName,
		data: data,
		dataType: 'json',
		success: function (data) {
			var status = data['Status'];
			if (status == 'success') {
				var article_widgets = data['article_widgets'];
				for (var i = 0; i < article_widgets.length; i++) {
					var widget = $('.article-collections.[data-collection-tag="' + articleTagName + '"]').attr('data-collection-loaded', 'true');
					widget.append(article_widgets[i]);
					widget.css('display', 'inline-block');
				}
				truncateArticleWidgetsDescriptions();
			}
		},
		error: function (xhr) {
			console.log('Unable to load.');
		}
	});
}

function truncateArticleWidgetsDescriptions(charNum) {
	var charNum = charNum || 150;

	$('.article-desc').each(function () {
		var articleDescriptionText = $(this).text(); // get the article description

		if (articleDescriptionText.length > charNum) { // if larger than charNum, truncate!
			var articleDescriptionTruncate = truncate(articleDescriptionText, charNum);
			$(this).html(articleDescriptionTruncate);
		}
	});
}

function truncate(text, maxLength) {
	var ret = text;
	if (ret.length > maxLength) {
		ret = ret.substr(0, maxLength - 3) + "...";
	}
	return ret;
}

function setToastrOptions() {
	toastr.options = {
		"positionClass": "toast-bottom-right",
		"preventDuplicates": false,
		"showDuration": "500",
		"hideDuration": "1000",
		"timeOut": "3000",
		"extendedTimeOut": "2000",
		"showMethod": "fadeIn",
		"hideMethod": "fadeOut"
	};
}

function setToastrErrorOptions() {
	toastr.options = {
		"positionClass": "toast-bottom-full-width",
		"preventDuplicates": false,
		"showDuration": "500",
		"hideDuration": "1000",
		"timeOut": "4000",
		"extendedTimeOut": "2000",
		"showMethod": "fadeIn",
		"hideMethod": "fadeOut"
	};
}

function bindOutOfStockToast() {

	$(".product-addon-checkbox-disabled")
		.unbind('click')
		.click(function () {
			setToastrOptions();
			toastr.options.preventDuplicates = true;
			toastr.warning('Oops! Item out of stock.');
		});
}

function helperLooksLikeMail(str) {
	var lastAtPos = str.lastIndexOf('@');
	var lastDotPos = str.lastIndexOf('.');
	return (lastAtPos < lastDotPos && lastAtPos > 0 && str.indexOf('@@') == -1 && lastDotPos > 2 && (str.length - lastDotPos) > 2);
}

function bindAvailableProductNoticeSignup() {
	$('.productNoticeSubscribeLink')
		.unbind('click')
		.click(function () {
			var product_key = $(this).attr('data-item-key');
			TINY.box.show({
				html: "<div class='modal-email-signup'><div class='form-group'><label for='EMAIL'>Notify me when this is available:</label><input type='email' class='form-control email productemail' id='mce-EMAIL' name='EMAIL' placeholder='Email Address'></div><div id='productAlertMessage' style='margin-top:10px;color:red;font-size:13px;'></div><div id='productAlertSubscribeLink' class='btn btn-subscribe-blue'>Signup</div></div>",
				animate: true,
				close: true,
				openjs: function () {
					bindProductAlertSignup(product_key);
				}
			});
		});
}

function bindProductAlertContinue() {
	$('#productAlertContinueLink')
		.unbind('click')
		.click(function () {
			$('#productAlertContinueLink').unbind('click');
			TINY.box.hide();
		});

}

function bindProductAlertSignup(product_key) {
	$('#productAlertSubscribeLink')
		.unbind('click')
		.html('Signup')
		.click(function () {
			$(this).unbind('click');
			$(this).html('Saving...');
			var email = $('.productemail').val();
			if (helperLooksLikeMail(email)) {
				$.ajax({
					type: "POST",
					url: '/handlers/email-signup',
					data: {
						email: email,
						signupType: 'product-notice',
						signupItem: product_key
					},
					dataType: 'json',
					success: function (data) {
						TINY.box.fill("<div style='padding:30px;'><div><span>Success! You will be notified when this item becomes available.</span><span id='productAlertContinueLink' class='btn btn-subscribe-blue' style='margin-bottom:20px;'>Continue</span></div>");
						bindProductAlertContinue();
					},
					error: function (xhr) {
						$('#productAlertMessage').html('Oops! We were unable to process.');
						TINY.box.fill($('.tcontent').html());
					}
				});
			} else {
				bindProductAlertSignup();
				$('#productAlertMessage').html('Oops! Please enter a valid email address.');
				TINY.box.fill($('.tcontent').html());
			}
		});
}

function bindNewsletterSignup() {
	$('#newsletterSubscribeLink')
		.unbind('click')
		.html("Subscribe");

	$('#newsletterSignupMessage').hide();



	// when pressing enter on form
	$('#mc-embedded-subscribe-form').bind('keypress keydown keyup', function (e) {
		var email = $('#mce-EMAIL').val();
		if (e.keyCode == 13) {
			e.preventDefault();
			processEmailSubscribe(email);
		}
	});

	// when clicking the subscribe button
	$('#newsletterSubscribeLink').click(function () {
		$(this).unbind('click');
		var email = $('#mce-EMAIL').val();
		processEmailSubscribe(email);
	});

	// process the sign-up
	function processEmailSubscribe(email) {
		if (helperLooksLikeMail(email)) {
			$('#newsletterSubscribeLink').html('Adding email to our list...').addClass('fade-50'); // temporarily change text and add a fade css property
			$('#newsletterSignupMessage').hide(); // hide error message if it was displayed

			$.ajax({
				type: "POST",
				url: '/handlers/email-signup',
				data: {
					email: email,
					signupType: 'newsletter'
				},
				dataType: 'json',
				success: function (data) {
					$('#mce-EMAIL').hide();
					setTimeout(
						function () {
							$('#newsletterSubscribeLink').addClass("button-submit-filled").html('Subscribed!').removeClass('fade-50');
						}, 1100);
				},
				error: function (xhr) {
				}
			});
		} else {
			bindNewsletterSignup();
			$('#mce-EMAIL').focus(); // re-focus the cursor back to the input
			$('#newsletterSignupMessage').show().html('Please enter a valid email.');
		}
	}
}

function productSingleLoadWidgets(num) {
	num = num || '50'; // sets default value if num wasn't passed to function
	var count = 0;
	var productLoadedFalse = $('.product-single[data-product-loaded="false"]');

	$(productLoadedFalse).each(function () { // iterate on product product keys to retrieve meta-data
		count++;
		var display_product = true;
		//Validates we should display product (only necessary on article related products)
		if ($('#productDisplayLimit').length) {
			if (display_count >= $('#productDisplayLimit').val()) {
				display_product = false;
			}
		}
		if (display_product) {
			if (count <= num) {
				var productKey = $(this).attr('data-product-key');
				getProductSingleWidget($(this), productKey); // ajax call to get data
			}
		}
	});
	var numProductLoadedFalse = $('.product-single[data-product-loaded="false"]').length; // count
	return numProductLoadedFalse - num; // return how many products are still not loaded   Aren't the products we just loaded already subtracted from this count? - Matt
}

function getProductSingleWidget(widget, productKey) {
	var is_loaded = $(this).attr('data-product-loaded');
	if (is_loaded !== 'true') {

		var widget_type = widget.attr('data-widget-type');
		var data = $('[data-product-key=' + productKey + ']').data();


		var post_url = '/product-single/' + productKey;
		if (widget_type == 'sku') {
			post_url = '/product-single/sku/' + productKey;
		} else if (widget_type == 'listing') {
			post_url = '/product-single/listing/' + productKey;
		}

		$.ajax({
			type: "POST",
			url: post_url,
			data: data,
			dataType: 'json',
			success: function (data) {
				var content = data['content'];
				//Validates we should display product (only necessary on article related products)
				var display_product = true;
				if (!content) //This should check for empty str, null, undefined, etc
				{
					display_product = false; //In the event of a blank product we don't want it to display in any form
				}
				if ($('#productDisplayLimit').length) {
					if (display_count >= $('#productDisplayLimit').val()) {
						display_product = false;
					}
				}
				if (~content.indexOf('product-outofstock')) {
						display_product = false;
					}
				if (display_product || !$('#productDisplayLimit').length) {
					++display_count;
					$('.product-single.[data-product-key=' + productKey + ']')
						.html(content)
						.attr('data-product-loaded', 'true')
						.attr('data-product-status', display_product?"available":"out-of-stock")
						.addClass('product-loaded');

					cartWidgetBindSearchAction();
					cartWidgetBindAddToCart();
					displayInStockProducts();

				}
			},
			error: function (xhr) {
				// $('[pkey=' + pkey + ']').html('unable to load');
				return;
			}
		});
	}
}

function cartWidgetBindSearchAction() {
	var search_link = $('.search-edit-link');
	if (search_link.length > 0) {
		$('.product-single')
			.unbind('click')
			.click(function () {
				var data_type = $(this).attr('data-widget-type');
				var item_key = $(this).attr('data-product-key');
				var search_term = search_link.attr('data-search-term');
				$.ajax({
					type: "POST",
					url: '/handlers/search-action',
					data: {
						itemKey: item_key,
						searchTerm: search_term,
						dataType: data_type
					},
					dataType: 'json',
					beforeSend: function (x) {
						if (x && x.overrideMimeType) {
							x.overrideMimeType("application/json;charset=UTF-8");
						}
					},
					success: function (data) {
						var status = data['status'];
					},
					error: function (xhr) {
						return;
					}
				});
			});

	}
}

function highlightSideNavigation() {
	var path = window.location.pathname;
	if (path.indexOf("/c/") != -1) {
		$('.sidebar-link').each(function (index) {
			var link = $(this).attr('href');
			if (link == path) {
				$(this).addClass('sidebar-link-selected');
				$(this).parent().parent().siblings('.sidebar-link').addClass('sidebar-link-selected');
			}
		});
	}
}

function bindProductSingleWidgetsScroll() {
	$(window).scroll(debounce(function () {
		numProductLoadedFalse = productSingleLoadWidgets(40); // while scrolling, keep loading products
		if (numProductLoadedFalse <= 0) {
			$(window).unbind('scroll'); // unbind the scroll!
		}
	}, 350));
}

function productCollectionLoadWidgets() {
	// iterate on product collection keys to retrieve meta-data
	$('.collection-single').each(function () {
		var collectionKey = $(this).attr('data-collection-key');
		// Make AJAX call to retrieve widget
		getProductCollectionWidget(collectionKey);

	});
}

function getProductCollectionWidget(collectionKey) {
	var data = $('[data-collection-key=' + collectionKey + ']').data();
	$.ajax({
		type: "POST",
		url: '/product-collection-widget/' + collectionKey,
		data: data,
		dataType: 'json',
		success: function (data) {
			var status = data['Status'];
			if (status == 'success') {
				var content = data['Content'];

				$('.collection-single.[data-collection-key=' + collectionKey + ']')
					.html(content)
					.attr('data-collection-loaded', 'true')
					.addClass('collection-loaded');
			}
			else {
				return;
			}
		},
		error: function (xhr) {
			console.log('Unable to load.');
		}
	});
}

function bindDownloadDigitalItem() {

	var design_list = $('#js-design-file-downloads');
	if (design_list.length > 0) {
		$('.digital-download-button').unbind('click');
		$('.digital-download-button').click(function () {
			$('.digital-item-error').hide();
			var download_button = $(this);
			download_button.html('Please wait...');
			setToastrOptions();
			toastr.info("Please wait...");
			var download_link = $(this).attr('data-download-link');
			var sku_key = $(this).attr('data-sku-key');
			$.ajax({
				type: "POST",
				url: '/handlers/digital-add-permission',
				data: {SkuKey: sku_key},
				dataType: 'json',
				beforeSend: function (x) {
					if (x && x.overrideMimeType) {
						x.overrideMimeType("application/json;charset=UTF-8");
					}
				},
				success: function (data) {
					var status = data['Status'];
					if (status == 'success') {
						window.location.href = download_link;
					} else if (status == 'login-required') {
						window.location.href = '/accounts/login?continue=' + window.location.href;
					} else {
						$('.digital-item-error').show();
					}
					download_button.html('Download');
				},
				error: function (xhr) {
					$('.digital-item-error').show();
					download_button.html('Download');
				}
			});
		});
	}
}

function bindAddToDigitalLibrary() {
	var add_to_library = $('.add-to-library-button');
	if (add_to_library.length > 0) {
		add_to_library.html('Add to Library');
		add_to_library.show();
		add_to_library.click(function () {
			var sku_key = $(this).attr('data-sku-key');
			$('.digital-item-error').hide();
			$(this).html('Please wait...');
			$(this).unbind('click');
			$.ajax({
				type: "POST",
				url: '/handlers/digital-add-permission',
				data: {SkuKey: sku_key},
				dataType: 'json',
				beforeSend: function (x) {
					if (x && x.overrideMimeType) {
						x.overrideMimeType("application/json;charset=UTF-8");
					}
				},
				success: function (data) {
					var status = data['Status'];
					if (status == 'success') {
						add_to_library.html('Items Added!');
					} else if (status == 'login-required') {
						window.location.href = '/accounts/login?continue=' + window.location.href;
					} else {
						bindAddToDigitalLibrary();
						$('.digital-item-error').show();
					}
				},
				error: function (xhr) {
					bindAddToDigitalLibrary();
					$('.digital-item-error').show();
				}
			});
		});
	}


	//var design_list = $('.design-file-list');
	//if (design_list.length > 0){

}

function cartWidgetBindAddToCart() {
	$('.add-to-cart-button')
		.unbind('click')
		.click(function () {
			var item_key = $(this).attr('data-item-key');

			var item_quantity = $(".add-to-cart-quantity[data-item-key='" + item_key + "']").val();

			var addons = [];
			$('[name=AddonProduct]').each(function () {
				if (($(this).is(':checked'))) {
					var sku_key = $(this).attr('data-skukey')
					if (sku_key == undefined) {
						//TODO: Implement popup sku selector for multiSku AddOns
						throw unimplemented
					}
					addon_string = $(this).attr('data-listingkey') + "," + sku_key
					addons.push(addon_string);
				}
			});

			addons = addons.join('|');
			$.ajax({
				type: "POST",
				url: '/handlers/cart-add',
				data: {
					itemQuantity: item_quantity,
					itemKey: item_key,
					addonarray: addons
				},
				dataType: 'json',
				beforeSend: function (x) {
					if (x && x.overrideMimeType) {
						x.overrideMimeType("application/json;charset=UTF-8");
					}
				},
				success: function (data) {
					var status = data['status'];
					_gaq.push(['_trackEvent', 'Cart', 'Add', item_key]);

					setToastrOptions();
					if (status == 'success') {
						$('.go-to-checkout-button').show().css('display', 'block');
						toastr.success('Item Added to Cart');
						$(this).html('Added to Cart');
						//If the page has a cart widget refresh it, otherwise reload
						var content = data['cart_widget'];
						var item_count = data['cart_item_count'];
						$('.js-cart-count').html(item_count);
					} else {
						$('.cart-error-message').html(data['error-message']);
						toastr.error(data['error-message']);
					}
				},
				error: function (xhr) {
					$('.cart-error-message').html('Unable to add item');
					setToastrOptions();
					toastr.error('Unable to add item');
					return;
				}
			});
		});
}

function bindProductSearch() {
	$('#js-nav-search-input').keyup(function (e) {
		if (e.which == 13) {
			e.preventDefault();
			$('#js-nav-search-button').click();
		}
	});

	$('#js-nav-search-button').click(function () {
		$(this).html('<span class="css-spinner css-spinner-white"></span>');
		var query = $('#js-nav-search-input').val();
		window.location.href = '/s/store?q=' + encodeURIComponent(query);
	});
}

function bindAddOns() {

	$('.addon-item').click(function () {
		var check = $(this).find('input[name=AddonProduct]');
		var is_checked = !check.is(':checked');
		check.attr('checked', is_checked);

		if (check.is(':checked')) {
			$(this).addClass('product-checkbox-is-checked');
			parent_price = ($('#price').html());
			parent_price = parent_price.replace('$', '');
			parent_price = parent_price.replace('USD', '');
			parent_price = parent_price.replace(',', '');
			parent_price = parseFloat(parent_price);
			child_price = parseFloat(check.attr('price'));
			new_price = child_price + parent_price;

			$('#price').html(new_price.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD (with add-ons)');

			list_parent_price = ($('#listprice').html());
			list_parent_price = list_parent_price.replace('$', '');
			list_parent_price = list_parent_price.replace('USD', '');
			list_parent_price = list_parent_price.replace(',', '');
			list_parent_price = parseFloat(list_parent_price);
			list_child_price = parseFloat(check.attr('listprice'));
			list_new_price = list_child_price + list_parent_price;

			$('#listprice').html(list_new_price.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD');
		} else {
			$(this).removeClass('product-checkbox-is-checked');
			parent_price = ($('#price').html());
			parent_price = parent_price.replace('USD', '');
			parent_price = parent_price.replace(',', '');
			parent_price = parseFloat(parent_price);
			child_price = parseFloat(check.attr('price'));
			new_price = parent_price - child_price;

			if ($('#addondiv input:checkbox:checked').length > 0) {
				$('#price').html(new_price.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD (with add-ons)');
			} else {
				$('#price').html(new_price.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD');
			}

			list_parent_price = ($('#listprice').html());
			list_parent_price = list_parent_price.replace('USD', '');
			list_parent_price = list_parent_price.replace(',', '');
			list_parent_price = parseFloat(list_parent_price);
			list_child_price = parseFloat(check.attr('listprice'));
			list_new_price = list_parent_price - list_child_price;

			$('#listprice').html(list_new_price.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD')
		}
	});

	$('.product-addon-checkbox').click(function () {
		var check = $(this).find('input[name=AddonProduct]');

		var originalPrice = parseFloat(stripFormat($('#price').attr('data-price')));
		var originalListPrice = parseFloat(stripFormat($('#listprice').attr('data-listprice')));

		// are any addons selected
		if ($('#product-addons input:checkbox:checked').length > 0) {
			var priceArray = [];
			var listPriceArray = [];

			// for each addon, add it up
			$('#product-addons input:checked').each(function () {
				priceArray.push(parseFloat($(this).attr('data-price')));
				listPriceArray.push(parseFloat($(this).attr('data-listprice')));

				var price = 0;
				var listPrice = 0;

				for (var i in priceArray) {
					price += priceArray[i];
				}
				for (var i in listPriceArray) {
					listPrice += listPriceArray[i];
				}

				// pull the word used in id="product-addons-title-word", otherwise set default
				var word = $("#product-addons-title-word").text();
				if (!word) {
					var word = "Add-ons";
				}

				$('#listprice').html(addFormat(listPrice + originalListPrice));
				$('#price').html(addFormat(price + originalPrice) + ' (with ' + word.toLowerCase() + ')');
			});

		} else {
			// default price and list price
			$('#listprice').html(addFormat(originalListPrice));
			$('#price').html(addFormat(originalPrice));
		}

		// strip out any commas, $, and USD
		function stripFormat(value) {
			if (!value || 0 === value.length) {
				value = 0;
			} else {
				var value;
				value.replace('$', '');
				value = value.replace('USD', '');
				value = value.replace(',', '');
			}

			return value;
		}

		// add $, commas, and USD
		function addFormat(num) {
			return num.toFixed(2).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' USD';
		}
	});
}

function bindScrollToAnchor() {
	// generic scroll to items on page (except for anything with the carousel class)
	$('a[href*="#"]:not([href="#"])').not('.carousel a').click(function () {
		if (location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '') && location.hostname == this.hostname) {
			var target = $(this.hash);
			target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
			if (target.length) {
				$('html, body').animate({
					scrollTop: target.offset().top - 30
				}, 1000);
				return false;
			}
		}
	});
}

// jquery plugin to shake form
(function ($) {
	$.fn.shake = function (options) {
		// defaults
		var settings = {
			'shakes': 2,
			'distance': 10,
			'duration': 250
		};
		// merge options
		if (options) {
			$.extend(settings, options);
		}
		// make it so
		var pos;
		return this.each(function () {
			$this = $(this);
			// position if necessary
			pos = $this.css('position');
			if (!pos || pos === 'static') {
				$this.css('position', 'relative');
			}
			// shake it
			for (var x = 1; x <= settings.shakes; x++) {
				$this.animate({left: settings.distance * -1}, (settings.duration / settings.shakes) / 4)
					.animate({left: settings.distance}, (settings.duration / settings.shakes) / 2)
					.animate({left: 0}, (settings.duration / settings.shakes) / 4);
			}
		});
	};
}(jQuery));

function bindSearchAffix() {
	$("#nav-search").affix({
		offset: {
			top: $("#js-nav").outerHeight(true)
		}
	});
}

/*! matchMedia() polyfill - Test a CSS media type/query in JS. Authors & copyright (c) 2012: Scott Jehl, Paul Irish, Nicholas Zakas, David Knight. Dual MIT/BSD license */
window.matchMedia || (window.matchMedia = function () {
	"use strict";

	// For browsers that support matchMedium api such as IE 9 and webkit
	var styleMedia = (window.styleMedia || window.media);

	// For those that don't support matchMedium
	if (!styleMedia) {
		var style = document.createElement('style'),
			script = document.getElementsByTagName('script')[0],
			info = null;

		style.type = 'text/css';
		style.id = 'matchmediajs-test';

		if (!script) {
			document.head.appendChild(style);
		} else {
			script.parentNode.insertBefore(style, script);
		}

		// 'style.currentStyle' is used by IE <= 8 and 'window.getComputedStyle' for all other browsers
		info = ('getComputedStyle' in window) && window.getComputedStyle(style, null) || style.currentStyle;

		styleMedia = {
			matchMedium: function (media) {
				var text = '@media ' + media + '{ #matchmediajs-test { width: 1px; } }';

				// 'style.styleSheet' is used by IE <= 8 and 'style.textContent' for all other browsers
				if (style.styleSheet) {
					style.styleSheet.cssText = text;
				} else {
					style.textContent = text;
				}

				// Test if media query is true or false
				return info.width === '1px';
			}
		};
	}

	return function (media) {
		return {
			matches: styleMedia.matchMedium(media || 'all'),
			media: media || 'all'
		};
	};
}());

function displayInStockProducts()
{
	$('.products-container').each(function() {
		// run this function in each product container
		in_stock_items = $(this).find('.product-single[data-product-status!=out-of-stock]:not(:empty)');
		in_stock_items.show();
		out_of_stock_items = $(this).find('.product-single[data-product-status=out-of-stock]:not(:empty)');
		out_of_stock_items.insertAfter(in_stock_items.last());
		out_of_stock_items.show();
	});
}
