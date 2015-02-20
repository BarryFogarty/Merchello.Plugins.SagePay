angular.module('merchello.plugins.sagepay').controller('Merchello.Plugin.Payment.SagePayPaymentProviderController',
    ['$scope',
    function ($scope) {

        console.info($scope.dialogData);
        
        if ($scope.dialogData.provider.extendedData.items.length > 0) {
            var extendedDataKey = 'merchSagePayProviderSettings';
            var settingsString = $scope.dialogData.provider.extendedData.getValue(extendedDataKey);
            $scope.sagePayProviderSettings = angular.fromJson(settingsString);
            console.info($scope.dialogData);
            console.info($scope.sagePayProviderSettings);

            // Watch with object equality to convert back to a string for the submit() call on the Save button
            $scope.$watch(function () {
                return $scope.sagePayProviderSettings;
            }, function (newValue, oldValue) {
                console.info(newValue);
                $scope.dialogData.provider.extendedData.setValue(extendedDataKey, angular.toJson(newValue));
            }, true);
        }
        
    }
]);